# Demo Ingestion Worker

## Uvod

Za implementacijo sem uporabil Docker, ker je bilo tako najlazje uprobiti MSSQL.

Lokalno okolje se lahko nadzira z `.env` in `Makefile` datotekama. Makefile je namenjen postavitvi, clenaupu in start/stop-u okolja. Zazenejo se lahko tudi testi lokalno na masini (potreben rocen build) ali pa docker containerju.

## MockAPI

V sklopu naloge sem tudi implementiral MockAPI, ki je neke vrste alternative predlaganemu JSON placeholder. V mojem primeru sem tako lazje simuliral napake. Te se pojavljajo nakljucno preko `.env`, kjer se nastavi kako pogosto zelim dobit retry exception.

## Delovanje Workerja

Samo delovanje workerja je s pomocje shranjevanja v vmesno datoteko. To sluzi kot neke vrste offload na drugi service, ce bi gledali down teh line. Tukaj je zelo preprost primer, kjer se shrani v neki temp file z hash-em in casom. Isti worker naceloma prebere API in shrani in takoj zatem iz te datoteke nalozi v bazo. Tukaj bi blo smiselno razsisrit se z enim workerjem, ampak nisem dodajal (to bi blo za produkcijo in skaliranje).  V mojem primeru se datoteka takoj brise, drugace bi se lahko pustila nekaj casa za vidljivost in se za tem brisala v batchu nekajkrajt na dan/teden (odvisno od loada).

## CI/CD

Dodal sem tudi simple CI/CD, ki pusha na github registry. Zbuilda in testira workerja in mock api in oba loceno pusha.

## Testiranje

Testi so v zelo okrnjeni obliki, idealno bi blo, da bi se loceno potestiral repository samo za bazo. Dobro bi blo met tudi load teste.

## Flow

Flow je taksen:

### Postavitev Lokalnega Okolja

```
MSSQL -> MockAPI -> Worker (ce sta oba dependant service healthy)
```

# Worker Flow

## Proces:

1. **Worker začne** (začetni klic)
    - Timer se sproži na vsakih 25 sekund (nastavljeno v .env)
    - Pokliče se ProcessOrdersAsync()

2. **MockAPI se odzove**
    - Worker naredi HTTP GET na MockAPI
    - API lahko vrne uspeh (200) ali napako (500, timeout, itd.)

3. **Worker reagira na error ali successful response**
    - Če je error:
        - Polly retry policy poskusi ponovno (5x)
        - Eksponenten backoff (2s, 4s, 8s, 16s, 32s)
        - Če vse faila -> Circuit Breaker odpre za 30s
    - Če je success:
        - Nadaljuje na naslednji korak

4. **Worker shrani v downloads folder**

   Tukaj se uporabita DVA service-a:

    - **OrderIngestionService**
        - Samo za pridobivanje podatkov z API-ja
        - Shrani v JSON datoteko (npr. order_batch_20251030_abc123.json)

    - **OrderLoaderService**
        - Samo za pushanje v bazo
        - Prebere iz te JSON datoteke

5. **Worker pusha v bazo in pobriše datoteko**
    - OrderRepository preveri če order že obstaja (gleda ExternalId)
    - Če ne obstaja -> INSERT v MSSQL
    - Če obstaja -> preskoči (duplikat)
    - Po uspešnem INSERT-u se JSON datoteka izbriše

   Opomba: Za delo z datotekami je Storage service, v realnem scenariju bi tukaj bil push na zunanji service (S3, Azure Blob Storage)

6. **Worker čaka na novi interval**
    - Počaka 25 sekund (ali karkoli je nastavljeno v .env)
    - Ponovi cel proces od začetka

   Prvotno sem nameraval 15 min interval, ampak ko sem testiral je bilo to prepočasi, zato sem dal 25s da lahko hitreje vidim rezultate.


## Dostop do MSSQL

MSSQL se lahko dostopa preko username/password:

```
jdbc:sqlserver://localhost:1433;databaseName=OrderIngestionDB;trustServerCertificate=true
```

## Infrastructure

V Infrastrcuture bi se manjkal Redis ali RabbitMQ, ce bi hoteli vse skupaj razsiriti. Redis sploh za rate limite in paginacijo. Trenutno je samo en klic in tudi responsi niso veliki. V kodi imam limit 50, lahko bi prestavil kot env, ampak sem zaenkrat pustil. Sicer je to nekaj kaj je odvisno od stregije endpointa in pipeline-a in tudi od uporabnikovega plana, zato bi mogla ta informacija prit iz baze.

RabbitMQ bi tukaj potrebovali za delegiranje eventov (branje, pisanje, transformacije). Razsirilo bi se lahko v workflowe z vecimi jobi. Jobi so neke vrste tudi lahko dependant odvisno od samega procesiranja. Tudi kdaj se naj prozi flow workerja namesto na fiksin itnerval 15 ali xzy minut, bi lahko takoj on demand ob eventu sli procesirat to zahtevo ob predpogoju, da imamo zunanji service, ki scheduale zahteve glede na doloceno configuration (staticno/dinamcino)

## Duplikati

V mojem primeru sem pridobil samo Orders entiteto, ko nekaj kja pride z API-ja in se zanasal na API entity id in glede na to razlikoval duplikate. To sem zapisal v Grafano.

## Grafana in Prometheus

Grafano sem uproabil kot alternativo App Insights. Z njo sem delal pred tem in je tudi Docker iamge, zato sem jo lahko samo uporabil. Sicer sem bolj kot ne delal z njo, nisem je maintal, zato sem si mogo malo pogledat kak ji mountant dashboarde. V kombinaciji sem se uproabil Promethues za tracing in Grafana UI samo scrape-a metrike, ki jih worker pusha v Promethues. V mojem primeru mam 3 total-e. Nato sem si naredo 3 grafe (preko UI-ja) in exportal JSON v grafana folder in lahko sluzi kot avtomatski dashaboard import. Primeri so v /screenshots

### Dostop do Grafane

Grafana se lahko testiral na http://localhost:3000/login (admin/admin), klikni "Skip", ker je default geslo, potem pa pod dsahboards. Sem dodal screenshote. To prikazuje isto kot logi v workerju. Zajema latest cifro, vidi pa se tudi after some time. Duplikati so tisti, ki so se spet v range-u MIN in MAX ponovno generirali in poslali. Sicer tukaj je tudi moznost, da se to vseeno shrani kot raw data v storage in nad tem naredi trasnformaciaj z SQL ali dbt (nism se se delal), da se posodablja v neki final view za druge potrebe. Kot neke vrste ELT ali pa ETL, odvisno od use case-a.

## CI/CD Test

Dodal sem en mini PR, da se vidi ko se prozi. Popravil sem README, samo da se zagrabi flow. En flow sem mergal in bi je blo dodano pod packages.

## Opomba

Naceloma bi uproabil Azure, ampak glede na to, da je bil incident na Microsoftu se kar stvari niso odpirale :)


### Hitra Postavitev

```bash
git clone <repository-url>
cd frodx_naloga

make help

# Build vseh Docker image-ov
make setup

# Zagon vsega skupaj
make start

docker-compose ps
```

### Primeri Uporabe

```bash
# Prva postavitev projekta
make setup
make start

# Ponovni zagon po spremembah kode
make stop
make setup
make start

# Popolno čiščenje in ponovna postavitev
make clean
make setup
make start

# Testiranje
make test-docker
make test
```

Storitve bodo dostopne na:
- **Worker Metrics**: http://localhost:8081/metrics
- **MockAPI**: http://localhost:5001/api/orders
- **Prometheus**: http://localhost:9090
- **Grafana**: http://localhost:3000 (admin/admin)
- **MSSQL**: localhost:1433 (sa/YourStrong@Passw0rd123)


## Konfiguracija

### ENV config

#### MSSQL Nastavitve
```bash
MSSQL_SA_PASSWORD=YourStrong@Passw0rd123   # Geslo za SA user-a
MSSQL_PORT=1433                           
MSSQL_DATABASE=OrderIngestionDB            
```

#### MockAPI Nastavitve
```bash
MOCK_API_PORT=5001                 
MOCK_API_ENABLE_ERRORS=true        # Omogoči naključne napake
MOCK_API_ERROR_RATE=0.35           # 35% verjetnost napake, vecinoma timeout ali internal error na API strani
MOCK_API_MIN_DELAY_MS=100          
MOCK_API_MAX_DELAY_MS=500          
```

#### Worker Nastavitve
```bash
WORKER_API_BASE_URL=http://mockapi:8080/api/orders  
WORKER_INGESTION_INTERVAL_SECONDS=25                # Kako pogosto se bo zagnal, spremeni po zelji
WORKER_RETRY_COUNT=5                                # Kolkokrat ponovimo, ce faila
WORKER_LOG_PATH=logs                                # Lokalni folder, kjer se shranjujejo logi
WORKER_DOWNLOAD_PATH=/tmp/order-ingestion/downloads # Pot do file-ov v katere shranimo po extractu z API-ja
WORKER_METRICS_PORT=8081                            # Potrebno za promethues, da lahko pridobiva metrike
```

#### Monitoring Nastavitve
```bash
PROMETHEUS_PORT=9090              
GRAFANA_PORT=3000                  
```

## Monitoring

### Grafana Dashboards

1. Odpri http://localhost:3000
2. Prijavi se z `admin/admin` in po lokalnem not safe promptu daj "Skip"
3. V levem sidebaru **Dashboards** → **Order Ingestion Monitoring** (glej screenshots mapo)

**Metrike na dashboardu:**
- **Total Orders Extracted** - Skupno število pridobljenih naročil
- **Total Orders Saved** - Skupno število shranjenih naročil
- **Total Duplicates** - Skupno število zaznanih duplikatov

Dashboard se osvežuje z minimalnim zamikom.

Quick Access -> http://localhost:3000/d/order-ingestion/order-ingestion-monitoring?orgId=1&from=now-15m&to=now&timezone=browser&refresh=5s

### Worker Metrike

Dostopne na: http://localhost:8081/metrics

**Custom metrike:**
- `orderingestion_orders_extracted_total`
- `orderingestion_orders_saved_total`
- `orderingestion_orders_duplicate_total`

## Struktura Projekta

```
frodx_naloga/
├── src/
│   ├── OrderIngestion.Worker/       # IngestionWorker
│   ├── OrderIngestion.Application/  # Logika servicov za extract, load
│   ├── OrderIngestion.Domain/       
│   ├── OrderIngestion.Infrastructure/ # MSSQL in WebAPI client
│   ├── OrderIngestion.Common/       # Za branje .env, custom exceptione, metrike
│   └── OrderIngestion.MockApi/      # Moj Mock Web API
├── tests/
│   └── OrderIngestion.Tests/        # Unit testi
├── grafana/
│   ├── dashboards/                  # JSON dashboard definicije
│   └── provisioning/                # Avtomatska konfiguracija
├── prometheus/
│   └── prometheus.yml               # Scraping konfiguracija
├── docker-compose.yml               
├── Makefile                         
└── .env                             
```

