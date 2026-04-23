# Notatka: realizacja zadań Z1–Z9

Mapowanie zadań z listy na konkretne miejsca w kodzie + opis algorytmów.

---

## Z1. Robots Exclusion Protocol

**Gdzie**: `WebCrawler.Robots/` i `WebCrawler.Domain/RobotsFile.cs`.

**Flow**:

`Program.cs` tworzy `new RobotsService(new RobotsFetcher(httpClient))`. Gdy `CrawlerEngine.CrawlAsync` startuje, pobiera plik raz na start (`_robots ??= await _robotsService.GetRobotsFileAsync($"https://{seedDomain}")`) i cache'uje go w polu prywatnym — nie pobiera ponownie dla każdego URL.

**Co robi każda klasa**:

- `RobotsFetcher.FetchRobotsAsync(url)` — zwykły `GET {url}/robots.txt` przez `HttpClient.GetStringAsync`. Łapie `HttpRequestException` (np. 404) i `TaskCanceledException` (timeout), w obu przypadkach zwraca `null` → crawler po prostu nie stosuje filtrowania.
- `RobotsParser.ParseRobots(host, content)` — parser linia-po-linii. Ignoruje komentarze (`#`) i puste linie, łapie dyrektywy `Allow:` i `Disallow:`, każdą pozycję konwertuje na `Regex` metodą `PatternToRegex`.
- `PatternToRegex` — escape'uje całą ścieżkę regex-owo, potem **podmienia `\*` → `.*`** i **`\$` → `$`** (wildcardy z protokołu robots). Jeśli wzorzec nie kończy się `$`, dokleja `.*` (dopasowanie prefiksowe). Regex compiluje się z `IgnoreCase`.
- `RobotsFile.IsAllowed(url)` — bierze `PathAndQuery` z URL-a i zwraca `!disallowed || allowed`. Jeśli URL wpada w jakiś `Disallow`, ale jednocześnie w jakiś bardziej szczegółowy `Allow` — dostęp przechodzi (standardowa semantyka REP).

**Gdzie jest wywoływana polityka**: `CrawlerEngine.CrawlSinglePageAsync` robi `if (_robots != null && !_robots.IsAllowed(url)) return;` zaraz po zdequeue'owaniu URL-a.

**Ograniczenie do zanotowania**: parser nie rozpoznaje sekcji `User-agent:` — reguły są globalne. Dla Warwick.ac.uk to działa, ale przy plikach z osobnymi blokami per bot warto byłoby rozbudować.

---

## Z2. Wielowątkowy crawler z kolejką zadań

**Gdzie**: `WebCrawler.Crawler/CrawlerEngine.cs`, `UrlFrontier.cs`, `UrlNormalizer.cs`, `LinkExtractor.cs`.

**Algorytm**: równoległy BFS z jedną wspólną kolejką URL-i i deduplikacją przy wrzucaniu.

**Frontier (BFS queue)** — `UrlFrontier`:

- `ConcurrentQueue<string>` dla samej kolejki (FIFO → BFS).
- `ConcurrentDictionary<string, byte>` jako zbiór "już widziane".
- `Enqueue(url)` atomowo sprawdza-i-wstawia do `_visited.TryAdd`; tylko jeśli URL był nowy, trafia do kolejki. Żadne URL nie trafi dwa razy (filtrowanie duplikatów jest na poziomie enqueue, nie dequeue).

**Normalizacja** — `UrlNormalizer.Normalize(raw, base)`:

1. `new Uri(new Uri(base), raw)` — resolve'uje względny adres (`/about`) do absolutnego.
2. Akceptuje wyłącznie `http` / `https`.
3. Usuwa fragment (`#section`), lowercase'uje host i scheme, sortuje parametry query (`SortQueryParams` — parsuje, sortuje po kluczu, składa z powrotem) — dzięki temu `?a=1&b=2` i `?b=2&a=1` to ten sam URL.
4. Usuwa domyślne porty (`:80`, `:443`).
5. `TrimEnd('/')` na końcu.

`GetDomain(url)` zwraca lowercased host — używane do filtra "same domain" w `CrawlerEngine`.

**Ekstrakcja linków** — `LinkExtractor.ExtractLinks(html, baseUrl)`:

- HtmlAgilityPack, XPath `//a[@href]`.
- Każdy href przechodzi przez `UrlNormalizer.Normalize(href, baseUrl)`; `null` (np. `mailto:`, `javascript:`) jest odrzucany.

**Silnik** — `CrawlerEngine`:

- `CrawlAsync(seedUrl)`: normalizuje seed, pobiera robots.txt, wrzuca seed do frontier, uruchamia N workerów przez `Enumerable.Range(0, ThreadCount).Select(_ => WorkerAsync(...))` + `Task.WhenAll`. Liczy czas Stopwatchem i drukuje throughput.
- `WorkerAsync`: pętla `while (_results.Count < MaxPages)`. Jeśli `TryDequeue` nie daje URL-a, zwiększa `emptyRetries`; po 10 pustych próbach (~1 s) worker się kończy — zabezpieczenie przed zakleszczeniem, gdy graf już został w pełni odwiedzony.
- `CrawlSinglePageAsync(url, frontier, allowedDomain)`:
  - `_robots.IsAllowed(url)` — filtrowanie po regułach.
  - `SendPolitelyAsync(request)` — politeness (patrz niżej).
  - Filtruje `Content-Type` na `text/html` (nie pobiera PDF-ów/obrazków).
  - `LinkExtractor.ExtractLinks` → dla każdego linka sprawdza `GetDomain(link) == allowedDomain` (ograniczenie do domeny seeda) i wrzuca do frontier.
  - Zapisuje wynik `new CrawlResult(url)` z `OutLinks` do `ConcurrentBag<CrawlResult> _results`.
  - Co 100 stron drukuje postęp.

**Politeness per-host** — `SendPolitelyAsync`:

To kluczowa część. Mamy prywatny `SemaphoreSlim _hostLock = new(1, 1)` i `DateTime _lastHitUtc`. Kiedy worker chce wysłać request, wchodzi do `_hostLock.WaitAsync()`, liczy `sinceLast = now - _lastHitUtc`; jeśli jest mniej niż `PolitenessDelay` — czeka resztę; stempluje `_lastHitUtc`, zwalnia lock, **dopiero wtedy** wysyła request. Dzięki temu serializujemy moment startu requestów, ale same requesty mogą lecieć równolegle (zwalniamy lock przed `SendAsync`). Efektywna częstotliwość do hosta = `1 / PolitenessDelay` niezależnie od liczby workerów.

**Testy wydajności** — `Program.RunPerformanceTests(targetUrl)`:

Iteruje po `{1, 2, 4, 8, 16, 32}` wątkach, dla każdego uruchamia świeży `CrawlerEngine` z `MaxPages=300`, mierzy Stopwatchem, liczy throughput (`pages / sec`) i speedup względem 1-wątkowego baseline'u. Na końcu drukuje tabelę. Wywołanie jest zakomentowane w `Main` — odkomentować przed pomiarami.

---

## Z3. Budowa grafu i podstawowe statystyki

**Gdzie**: `WebCrawler.Graph/Building/GraphBuilder.cs` (budowa), `WebCrawler.Domain/CrawlGraph.cs` (struktura), `WebCrawler.Graph/Reporting/GraphReporter.cs` i `Charts/HistogramChart.cs` (statystyki + wykresy).

**Budowa grafu** — `GraphBuilder.Build(List<CrawlResult>)`:

1. Każdemu URL-owi przypisuje indeks (`urlToIndex`, `indexToUrl`) — wierzchołki są `int`-ami, szybsze niż stringi.
2. Inicjalizuje listy sąsiedztwa `adjacency` i `adjacencyReversed` (obie `List<List<int>>`, po jednej liście per wierzchołek).
3. Dla każdego `CrawlResult` iteruje po jego `OutLinks`; jeśli link prowadzi do URL-a, którego **nie odwiedziliśmy** (nie ma go w `urlToIndex`), jest pomijany — graf ogranicza się do zbioru odwiedzonych stron.
4. Zwraca `CrawlGraph` z oboma kierunkami plus mapami URL↔indeks.

`CrawlGraph` sam liczy: `V` (= `indexToUrl.Count`), `E` (suma `neighbors.Count`), `OutDegrees[v]` i `InDegrees[v]` (z `adjacency` i `adjacencyReversed`).

**Statystyki** — `GraphAnalysisService.RunBasicStatistics()`:

- Woła `GraphReporter.PrintBasicStats(graph)` — drukuje `|V|`, `|E|`, średni out-degree (`E/V`), średni in-degree (= out-degree dla grafu skierowanego), gęstość (`E / V(V-1)`).
- `HistogramChart.Render(...)` × 2 — liniowe histogramy in/out-degree (`in-degree.png`, `out-degree.png`). Grupowanie po stopniu, ScottPlot `Add.Bars`, thinning etykiet X gdy wartości jest dużo (`minGap`-owa heurystyka).
- `HistogramChart.RenderLogLog(...)` × 2 — log-log scatter (`in-degree_loglog.png`, `out-degree_loglog.png`). Zera są odrzucane (log(0) jest niezdefiniowany), na osiach `log₁₀(k)` i `log₁₀(count)`.
- `RunPowerLawFits(...)` × 2 — patrz Z5.

---

## Z4. Składowe spójności i struktura bow-tie

**Gdzie**: `Analysis/SCCFinder.cs`, `WCCFinder.cs`, `ComponentAnalyzer.cs`, `BowTieAnalyzer.cs`, `Building/CondensationBuilder.cs`, plus reportery.

**SCC — algorytm Kosaraju** (`SCCFinder.FindKosaraju`):

1. Pierwsze DFS (`Dfs1`) po grafie `Adjacency` — wierzchołki są dodawane do listy `order` w kolejności post-order.
2. Reverse `order`.
3. Drugie DFS (`Dfs2`) po grafie **odwróconym** (`AdjacencyReversed`) w tej odwróconej kolejności. Każde drzewo DFS to jedna SCC; przydzielamy kolejne `sccId` do `comp[u]`.

Zwraca `Dictionary<int,int>` (node → sccId).

**WCC** (`WCCFinder.FindWCC`): DFS traktujący graf jako nieskierowany (iteruje jednocześnie `Adjacency` i `AdjacencyReversed`). Każde drzewo DFS = jedna WCC.

**Fasada DTO** — `ComponentAnalyzer.AnalyzeScc` / `AnalyzeWcc` opakowują surowy dict w `ComponentAnalysis` (Kind + NodeToComponent), które z kolei wylicza: `Sizes` (histogram), `LargestSize`, `LargestComponentNodes`.

**Bow-tie** — `BowTieAnalyzer.Analyze`:

1. Liczy SCC i bierze największą jako `CORE`.
2. `OUT = BFS-forward(CORE) \ CORE` — wierzchołki osiągalne z CORE po krawędziach skierowanych (`graph.Adjacency`).
3. `IN = BFS-backward(CORE) \ CORE` — wierzchołki z których można dotrzeć do CORE (BFS po `AdjacencyReversed`).
4. `TENDRILS = V \ (CORE ∪ IN ∪ OUT)` — wszystko, co zostało.

Pomocnik `TraverseBfs(graph, start, reversed)` to zwykły BFS, tylko wybiera kierunek sąsiedztwa.

**Kondensacja (DAG)** — `CondensationBuilder.Build(graph, comp)`:

Iteruje po wszystkich krawędziach oryginalnego grafu; jeśli `comp[u] != comp[v]`, dodaje krawędź `comp[u] → comp[v]` w `CondensationGraph` (używa `HashSet<int>` żeby krawędzie były unikalne). DAG z definicji — krawędzie między SCC nie mogą tworzyć cyklu.

**Raportowanie**: `ComponentReporter.Print` drukuje liczbę komponentów i rozmiar największego; `HistogramChart` rysuje `SCC.png` i `WCC.png`; `BowTieReporter` drukuje liczebności CORE/IN/OUT/TENDRILS; `GraphAnalysisService.BuildCondensation` drukuje `|V|` i `|E|` DAG-a.

**Uwaga implementacyjna**: oba `FindKosaraju` i `FindWCC` używają rekurencyjnego DFS. Przy 3000 wierzchołkach C# zwykle wystarcza, ale przy długich łańcuchach ryzyko StackOverflow istnieje — do rozważenia przepisanie iteracyjne (tak jak w `ArticulationFinder`).

---

## Z5. Rozkłady stopni — prawo potęgowe

**Gdzie**: `Analysis/PowerLawFitter.cs`, `Charts/PowerLawChart.cs`, wywoływane z `GraphAnalysisService.RunPowerLawFits`.

**Metoda OLS** — `PowerLawFitter.FitOls(degrees)`:

Opiera się na CCDF: P(K ≥ k) maleje potęgowo z wykładnikiem γ−1 jeśli PDF maleje jak k^(−γ).

1. Grupuje stopnie > 0, liczy dla każdego odrębnego k ile wierzchołków ma `degree = k` (zbudowana `groups`).
2. Liczy CCDF: dla każdego k `ccdf[i] = (Σ count[j]; j ≥ i) / N` (jedna pętla od końca).
3. Liczy `log(k)` i `log(CCDF(k))`.
4. Klasyczna regresja liniowa OLS: `slope = ssXY / ssXX`, `intercept = ȳ − slope·x̄`. Współczynnik γ dla PDF to `1 - slope` (przejście z CCDF do PDF).
5. R² = `ssXY² / (ssXX · ssYY)`.

Zwraca `OlsPowerLawFit(gamma, r2, logK, logCcdf, slope, intercept)`.

**Metoda MLE** — `PowerLawFitter.FitMle(degrees, minTailSize=50)`:

Klasyczny Clauset-Shalizi-Newman:

1. Dla każdego możliwego `xMin` (przechodzi po odrębnych wartościach stopnia):
   - `tail = degrees.Where(x => x >= xMin)`; jeśli krótszy niż `minTailSize`, skip.
   - MLE dla dyskretnego rozkładu potęgowego: `γ = 1 + n · (Σ log(xᵢ / xMin))^(-1)`.
   - Oblicza statystykę Kolmogorova-Smirnova między empiryczną CDF ogona i teoretyczną `1 - (x/xMin)^(1-γ)` — supremum różnicy bezwzględnej.
2. Wybiera `xMin` z najmniejszym KS.

Zwraca `MlePowerLawFit(gamma, ks, xMin, tail)`.

**Wykresy** — `PowerLawChart`:

- `RenderOls` — scatter `(log k, log CCDF)` + nałożona linia regresji (styl OLS: `y = slope·x + intercept`, γ i R² w legendzie).
- `RenderMle` — wykres empirycznej CDF ogona vs teoretyczna CDF (linia ciągła vs przerywana), w legendzie γ, xMin, KS.

**Raportowanie**: `GraphReporter.PrintPowerLawOls` / `PrintPowerLawMle` drukują wartości; `RunPowerLawFits("In-degree"/"Out-degree", ...)` robi to oba razy. W dokumentacji można porównać wynik z literaturą: γin ≈ 2.1, γout ≈ 2.7.

---

## Z6. Najkrótsze ścieżki i odległości

**Gdzie**: `Analysis/BFSExplorer.cs`, `Results/BfsAnalysis.cs`, `Charts/BfsCharts.cs`, `Reporting/BfsReporter.cs`, orkiestracja w `GraphAnalysisService.RunBfsAnalysis`.

**Rdzeń** — `BFSExplorer.Explore(graph, active=null)`:

Uruchamia BFS z każdego aktywnego wierzchołka:

1. Dla każdego `s`: `Bfs(graph, s, active)` zwraca `(dist[], parent[])`. BFS standardowy: `Queue<int>`, tablica `dist` inicjalizowana `-1`, dla każdego sąsiada nieosiągniętego `dist[v] = dist[u] + 1`.
2. Po BFS iteruje wszystkie `dist[v] > 0`:
   - Aktualizuje `sum` i `count` → `avgDist[s] = sum / count`.
   - Śledzi maksymalne `d` → `ecc[s]`.
   - Inkrementuje `pairHistogram[d]` (tablica `long[]` rosnąca przez `Array.Resize`, żeby pomieścić ewentualnie duże odległości).
3. `diameter = max(ecc)`, `radius = min(ecc)`.
4. Na końcu trymuje `pairHistogram` do ostatniego niezerowego indeksu.

Zwraca `BfsAnalysis(avgDistPerNode, eccentricity, diameter, radius, pairDistanceHistogram)`. DTO ma też property `GlobalAverageDistance` (średnia `avgDistPerNode` z pominięciem −1) i `ReachablePairs` (suma histogramu).

**Regresja + small-world** — w `GraphAnalysisService.FitDistanceHistogram`:

OLS na punktach `(d, log₁₀(count[d]))` dla `d ≥ 1, count > 0`. Liczy slope, intercept, R² standardowymi wzorami sum. Ujemny slope oznacza wykładniczy zanik; im bardziej ujemny, tym bardziej "skoncentrowane" odległości.

**Raport** — `BfsReporter.Print(bfs, nodeCount, slope, r2)`:

Drukuje: średnią odległość, średnicę, promień, liczbę osiągalnych par. Potem small-world check: `avgDistance / ln(N)` i `avgDistance / log₂(N)` — wartości bliskie 1 potwierdzają efekt małego świata. Dalej parametry regresji (`slope`, `R²`) i podgląd pierwszych 10 `avgDist` / `ecc`.

**Wykresy**:

- `HistogramChart.Render("Eccentricity distribution", ...)` — `Eccentricity.png` (liniowy histogram słupkowy).
- `BfsCharts.RenderPairDistanceHistogram(histogram, slope, intercept, "pair_distance_histogram.png")` — słupki + nałożona linia regresji `10^(slope·d + intercept)`.

---

## Z7. Współczynniki klasteryzacji

**Gdzie**: `Analysis/ClusteringAnalyzer.cs`, `Results/ClusteringAnalysis.cs`, `Charts/ClusteringCharts.cs`, `Reporting/ClusteringReporter.cs`.

**Lokalny C(v)** — `ClusteringAnalyzer.ComputeLocalCoefficient`:

Dla wierzchołka v o stopniu (out-adjacency) `k`:

- Jeśli `k < 2`: zwraca `-1` (niezdefiniowane).
- Liczy `connections` — liczbę par sąsiadów (n, nn), gdzie `nn` jest sąsiadem `n` i zarazem sąsiadem `v`. Podwójna pętla po sąsiadach.
- `C(v) = connections / (k · (k−1))` (pełny mianownik bez `/2`, bo każde ogniwo liczymy dwa razy przy podwójnej iteracji).

`ComputeLocalCoefficients` wywołuje to dla wszystkich `v` i zwraca tablicę.

**Globalny C** — `ComputeGlobalCoefficient` (transitivity):

Iteruje wszystkie wierzchołki:

- `allTriplets += k · (k−1) / 2` (liczba par sąsiadów = liczba możliwych trójkątów przez v).
- `closedTriplets`: dla każdej pary sąsiadów `(n, m)` sprawdza czy `m ∈ Adjacency[n]` (krawędź domykająca trójkąt); jeśli tak, inkrementuje. Warunek `n < m` zapobiega podwójnemu liczeniu.

`C = closedTriplets / allTriplets`.

**Zależność C(k) vs k** — `ClusteringAnalyzer.Analyze`:

- Buduje `avgByDegree` — dla każdego odrębnego stopnia k średnią z C(v) dla wierzchołków o tym stopniu.
- `FitLogLog(avgByDegree)` — klasyczna regresja OLS na `(log₁₀ k, log₁₀ C(k))`. Zwraca slope, intercept.

Zwraca `ClusteringAnalysis(localCoefficients, global, averageCByDegree, logLogSlope, logLogIntercept)`.

**Wykresy** — `ClusteringCharts`:

- `RenderLocalHistogram` — 10 binów szerokości 0.1, histogram słupkowy lokalnych C(v) (pomija `-1`). Zapis `LocalClusteringCoefficients.png`.
- `RenderLogLog` — scatter `(log k, log C(k))` + linia regresji. Zapis `ck_loglog.png`.

**Raport** — `ClusteringReporter.Print`:

Drukuje globalne C, slope log-log (oczekiwane ok. −1 dla sieci "hierarchicznych") i współczynnik wyrazu wolnego `10^intercept`.

---

## Z8. PageRank

**Gdzie**: `Analysis/PageRanker.cs`, `Algorithms/VectorNorms.cs`, `Results/PageRankAnalysis.cs`, `Charts/PageRankCharts.cs`, `Reporting/PageRankReporter.cs`.

**Algorytm** — `PageRanker.Compute(graph, norm, d=0.85, epsilon=1e-6)`:

1. Inicjalizuje `x[v] = 1/N` dla wszystkich wierzchołków.
2. Pętla iteracyjna:
   - `danglingSum = Σ x[v]; dla v s.t. OutDegrees[v] == 0` — łapie wierzchołki bez linków wychodzących, żeby ich "masa" nie znikała (redystrybuujemy ją równomiernie).
   - Dla każdego `v`:
     ```
     xNew[v] = (1 - d) / N                    // teleportacja
             + d · danglingSum / N            // rozrzut danglingów
             + d · Σ (x[u] / OutDegrees[u])  // wkład od u ∈ sąsiadów w odwróconym grafie
     ```
     Suma idzie po `graph.AdjacencyReversed[v]` (wszystkie u linkujące do v).
   - `err = norm.Fn(xNew, x)`, zapisujemy `err` w `errors` (ścieżka zbieżności).
   - `Array.Copy(xNew, x, V)` i jeśli `err < epsilon` — koniec.
3. Zwraca `PageRankAnalysis(scores, iterations, dampingFactor, normName, convergencePath)`.

**Normy wektorowe** — `Algorithms/VectorNorms`:

- `NormL1(a, b) = Σ |aᵢ − bᵢ|`
- `NormL2(a, b) = √(Σ (aᵢ − bᵢ)²)`
- `NormLInf(a, b) = max |aᵢ − bᵢ|`

Wszystkie podpięte przez delegat `NormFunc`. `All` to lista trzech krotek `(Name, Fn)` — używane w orkiestracji do iteracji po wszystkich normach.

**Współczynniki tłumienia**: `PageRanker.DampingFactors = {1.00, 0.99, 0.95, 0.90, 0.85, 0.70, 0.50}` (d=1 wymaga działającej obsługi danglingów, bo inaczej masa by zanikała).

**Orkiestracja** — `GraphAnalysisService.RunPageRankAnalysis(topCount=20)`:

1. **Iteracje do zbieżności vs d** — dla każdej normy odpala PageRank z każdym d z listy; notuje liczbę iteracji. `PageRankCharts.RenderIterationsVsDamping` rysuje `log₁₀(iterations) vs d` jako osobne serie per norma (`pr_iters_vs_d.png`).
2. **Referencja** — jeden run z L1, d=0.85. `PageRankReporter.PrintConvergence` drukuje liczbę iteracji do uzyskania `ε < 10⁻⁶`.
3. **Log-log rozkład** — `FitPageRankLogLog` liczy regresję OLS na `(log₁₀(rank), log₁₀(PR))` posortowanych malejąco. `PageRankCharts.RenderDistributionLogLog` rysuje `pr_loglog.png` z linią regresji. `PageRankReporter.PrintDistributionFit` drukuje slope.
4. **Porównanie norm** — ten sam run PageRank z każdą z trzech norm (d=0.85), `PageRankCharts.RenderNormConvergence` rysuje `log₁₀(err)` vs iteracja dla każdej normy (`pr_norm_convergence.png`).
5. **Top-20** — sortowanie `scores` malejąco, pierwsze 20, `PageRankReporter.PrintTopN` drukuje (d, norma, lista), `PageRankCharts.RenderTopN` rysuje słupki (`pr_top20.png`).

---

## Z9. Odporność na awarie i ataki

**Gdzie**: `Analysis/RobustnessAnalyzer.cs`, `Analysis/ArticulationFinder.cs`, `Results/RobustnessAnalysisResult.cs`, `Charts/RobustnessChart.cs`, `Reporting/RobustnessReporter.cs`, `Reporting/ConnectivityReporter.cs`.

**Symulacja usuwania** — `RobustnessAnalyzer.SimulateRemoval(graph, strategy, fractions=null)`:

Strategie (enum `RemovalStrategy`):

- `Random` — losowe awarie. Samplowanie bez powtórzeń przez Fisher-Yates partial shuffle (`SampleWithoutReplacement`): kopiuje active do tablicy, dla `i = 0..count-1` losuje `j = rnd.Next(i, n)` i zamienia `array[i]` ↔ `array[j]`, bierze pierwsze `count` elementów. O(count), gwarantuje unikalność.
- `TargetedAttack` — ataki. `active.OrderByDescending(v => InDegrees[v]).Take(toRemoveCount)` — atakuje huby.

Iteruje po fractions `{0.01, 0.02, 0.05, 0.10, 0.20, 0.30, 0.50}`. Każdy krok:

1. `toRemoveCount = graph.V · fraction - removedCount` (kumulatywnie).
2. Wybiera wierzchołki do usunięcia (strategia).
3. Usuwa z `active` (HashSet).
4. Liczy na podgrafie aktywnych:
   - `ComponentAnalyzer.AnalyzeWcc(graph, active)` → największy rozmiar WCC.
   - `ComponentAnalyzer.AnalyzeScc(graph, active)` → największy rozmiar SCC.
   - `BFSExplorer.Explore(graph, largestWccNodes)` → avg distance i diameter w największej WCC.
   - `ComputeActiveDegrees(graph, active)` — dla każdego aktywnego wierzchołka liczy in/out-degree **ograniczony do aktywnych** (pomija krawędzie do usuniętych).
5. Dodaje `RobustnessAnalysisResult(fraction, largestWCC, largestSCC, avgDist, diameter, inDegrees, outDegrees)`.

DTO ma jeszcze pochodne: `AvgInDegree`, `AvgOutDegree`, `MaxInDegree`, `MaxOutDegree`.

**Raportowanie** — `RobustnessReporter.Print(scenario, results)`:

Drukuje tabelę z kolumnami `f | WCC | SCC | avg dist | diam | avg in | max in | avg out | max out` per krok.

**Wykresy** — `RobustnessChart`:

- `Render(results, totalNodes, scenarioLabel, filename)` — krzywe **względnego** rozmiaru największej WCC i SCC (`size / totalNodes`) vs frakcja usuniętych. `robustness_wcc_scc_random.png`, `robustness_wcc_scc_attack.png`.
- `RenderDegreeEvolution(results, scenarioLabel, filename)` — cztery krzywe: `avg in-degree`, `avg out-degree` (pełne markery), `max in-degree`, `max out-degree` (puste markery, linia przerywana) vs frakcja. Pokazuje jak atak vs awaria wpływa na rozkład stopni. `robustness_degree_random.png`, `robustness_degree_attack.png`.

**Wierzchołki/krawędzie rozspajające** — `ArticulationFinder.Find(graph)`:

**Iteracyjny** Hopcroft-Tarjan na nieskierowanej wersji grafu. Używa jawnego stosu bo rekurencyjny DFS wywala się stackiem przy długich ścieżkach.

1. Budowa list sąsiadów jako `Adjacency ∪ AdjacencyReversed` (HashSet per wierzchołek, bez self-loopów).
2. Tablice: `disc[v]` (discovery time), `low[v]` (najmniejszy `disc` osiągalny z poddrzewa v), `parent[v]`.
3. Dla każdego nieodkrytego `start`:
   - Stos `(node, iter)` — aktualna pozycja iteratora po sąsiadach.
   - Pop `(u, i)`:
     - Jeśli `i < neighbours[u].Count`: pushuj `(u, i+1)` z powrotem (żeby wrócić do reszty sąsiadów po przetworzeniu tego), potem:
       - Sąsiad `v` nieodkryty → `parent[v] = u`, `disc[v] = low[v] = timer++`, push `(v, 0)`.
       - `v` odkryty i ≠ parent[u] → back edge; `low[u] = min(low[u], disc[v])`.
     - Jeśli `i == count`: post-order dla u. Aktualizuj `low[parent[u]] = min(low[parent[u]], low[u])`. Sprawdź reguły:
       - **Articulation (non-root)**: `low[u] >= disc[parent[u]]` → `parent[u]` jest punktem artykulacji.
       - **Bridge**: `low[u] > disc[parent[u]]` → krawędź (parent[u], u) jest mostem.
   - Po skończeniu DFS: jeśli `start` miał ≥ 2 tree-children → `start` też jest punktem artykulacji (reguła dla korzenia).
4. Zwraca `ConnectivityAnalysis(articulationPoints, bridges)`.

**Raport** — `ConnectivityReporter.Print`: liczba punktów artykulacji, liczba mostów, top-10 przykładów każdego.

**Wywoływane w `Program.cs`** jako `analysis.RunConnectivityAnalysis()` — uruchamiane przed `RunRobustnessAnalysis()`, żeby mieć jeden wspólny "raport odporności strukturalnej".

---

## Mapa plików w skrócie

```
WebCrawler.Robots/
  RobotsFetcher.cs       Z1  pobieranie robots.txt
  RobotsParser.cs        Z1  parsing + wildcardy → Regex
  RobotsService.cs       Z1  fasada
WebCrawler.Domain/
  RobotsFile.cs          Z1  IsAllowed
  CrawlerOptions.cs      Z2  MaxPages, ThreadCount, PolitenessDelay, UserAgent
  CrawlGraph.cs          Z3  V, E, Adjacency, AdjacencyReversed, stopnie
  CondensationGraph.cs   Z4  DAG SCC
  CrawlResults.cs        Z2  CrawlResult(Url, OutLinks)
WebCrawler.Crawler/
  UrlFrontier.cs         Z2  ConcurrentQueue + dedup
  UrlNormalizer.cs       Z2  Normalize + SortQueryParams + GetDomain
  LinkExtractor.cs       Z2  HtmlAgilityPack XPath
  CrawlerEngine.cs       Z2  workers, SendPolitelyAsync
WebCrawler.Graph/
  Building/
    GraphBuilder.cs          Z3
    CondensationBuilder.cs   Z4
  Analysis/
    SCCFinder.cs             Z4  Kosaraju
    WCCFinder.cs             Z4  DFS undirected
    ComponentAnalyzer.cs     Z4  fasada + DTO
    BowTieAnalyzer.cs        Z4  CORE/IN/OUT/TENDRILS
    PowerLawFitter.cs        Z5  OLS + MLE
    BFSExplorer.cs           Z6  BFS + histogram par
    ClusteringAnalyzer.cs    Z7  local/global C + log-log
    PageRanker.cs            Z8  iteracyjny PR + dangling
    RobustnessAnalyzer.cs    Z9  random/attack symulacja
    ArticulationFinder.cs    Z9  Hopcroft-Tarjan iteracyjny
  Algorithms/
    VectorNorms.cs           Z8  L1, L2, L∞
  Results/                   DTOs dla każdego analyzera
  Charts/                    ScottPlot — tylko renderowanie
  Reporting/                 Console — tylko drukowanie
  GraphAnalysisService.cs    orkiestrator analyzer → DTO → reporter + chart
WebCrawler.Console/
  Program.cs             main: crawl + pełny pipeline analizy
```
