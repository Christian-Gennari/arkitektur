# Presentation: NordPost och Event-driven Architecture

Förslag på en presentation på 3–5 minuter. Kör `dotnet run` i Development-miljö och
öppna adressen som visas i terminalen.

## 1. Verksamhetsproblemet

> När en paketoperatör registrerar eller levererar en försändelse måste flera system
> reagera: publik spårning, kundnotifieringar, verksamhetsstatistik och revision. Om
> ShipmentService anropar alla direkt blir den hårt kopplad till varje system.

Visa de fyra subscribers i arkitekturkortet.

> I NordPost publicerar producenten i stället ett domänevent om vad som redan har hänt.
> Subscribers väljer själva vilka events de reagerar på och känner inte till varandra.

## 2. Registrera en försändelse

Registrera exempelvis mottagaren **Ada Lovelace** med destination **Stockholm 111 22**.

> HTTP-delen är synkron: API:et validerar och sparar försändelsen. När
> ShipmentRegistered accepterats av kön kan API:et svara 201. Försändelsen syns direkt,
> men operationsräknaren och publik tracking kan fortfarande ligga efter.

Peka på den markerade asynkrona gränsen och eventet i Channel-kön.

> Det är eventual consistency. Huvudoperationen är klar innan subscribers har byggt
> sina egna read models. Development-konfigurationens simulerade latens gör detta
> synligt; den är inte ett krav i arkitekturen.

## 3. Visa fan-out

När eventet lämnar kön, peka på de fyra subscribers som lyser upp.

> BackgroundService hämtar eventet och dispatchern skickar samma oföränderliga snapshot
> till fyra oberoende mottagare. De startar nästan samtidigt men avslutas vid olika
> tidpunkter: audit och metrics är snabba, tracking tar lite längre tid och ett externt
> notifieringsgateway är långsammast. Tiderna visas per subscriber i Event Monitor.

## 4. Fortsätt livscykeln

Klicka **Dispatch** och därefter **Mark delivered**.

> Varje giltig domänövergång producerar ett specifikt event: ShipmentDispatched och
> ShipmentDelivered. En ogiltig övergång ger 409 och publicerar inget event. Samma
> pipeline och samma subscribers återanvänds utan specialkoppling i producenten.

## 5. Avgränsningar

> Detta är lokal EDA. Channel-kön och subscribers kör i samma process och data ligger i
> minnet. Vid omstart kan köade events försvinna, och demon saknar retries och dead-letter
> queue. En produktionsversion skulle använda en beständig broker och Outbox Pattern,
> idempotenta consumers samt distribuerad tracing.

## Vanliga frågor

**Är det EDA utan Kafka eller RabbitMQ?**

Ja. Producenten publicerar utan att känna sina mottagare och bearbetningen sker
asynkront. En extern broker behövs för robust distribution, inte för själva principen.

**Varför används fortfarande HTTP?**

HTTP passar användarens commands och queries. Events används där flera oberoende system
ska reagera efter att domänoperationen är genomförd.

**Vad händer om en subscriber misslyckas?**

Felet markeras i Event Monitor, medan övriga subscribers fortsätter. Demon gör inga
automatiska retries.

**Varför finns både shipment-listan och public tracking?**

Shipment-listan visar det primära command-tillståndet direkt. Public tracking är en
separat subscriber-byggd read model och kan därför tillfälligt ligga efter.
