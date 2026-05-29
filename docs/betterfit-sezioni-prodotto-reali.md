# Betterfit - Sezioni prodotto reali

## 1. Scopo del documento

Questo documento definisce quali sezioni devono esistere davvero nel prodotto Betterfit prendendo come riferimento:

- la base gia presente nel repository;
- gli screenshot del gestionale attuale usato come benchmark;
- il fatto che Betterfit deve restare un prodotto piu pulito, coerente e mantenibile del software di partenza.

Obiettivo: decidere una struttura di prodotto reale, non una copia 1:1 del gestionale esistente.

---

## 2. Principio guida

Negli screenshot ci sono molte funzioni utili, ma sono distribuite in troppi menu e con troppi livelli di navigazione.

Per Betterfit conviene adottare questa regola:

- tenere top-level solo i moduli che aprono un vero flusso operativo;
- spostare dentro il profilo utente tutto cio che ha senso solo nel contesto di una persona;
- trattare alcune funzioni come opzionali o di seconda fase;
- evitare moduli "ornamentali" o troppo verticali se non sono ancora centrali per il business.

In pratica Betterfit non deve avere 15 sezioni principali. Deve avere pochi moduli forti e sottosezioni molto chiare.

---

## 3. Navigazione principale raccomandata

Le sezioni top-level consigliate per la dashboard staff sono queste:

1. `Home`
2. `Utenti`
3. `Vendite`
4. `Accessi`
5. `Attivita`
6. `Training`
7. `CRM`
8. `Analytics`
9. `Impostazioni`

Modulo opzionale separato solo se realmente attivato:

10. `Dispositivi`

Questa struttura e piu corretta del benchmark, perche:

- `Ricevute e Spese` non deve stare separato da `Vendite`;
- `Listino` non deve essere top-level, ma figlio di `Vendite`;
- `Gestione corsi e campi` va rinominato e pulito dentro `Attivita`;
- `Planning PT` non deve vivere come modulo isolato, ma dentro `Training` o `Attivita`;
- `Consigli alimentari`, `Documenti`, `Anamnesi`, `Accessi`, `Pagamenti` e simili devono vivere soprattutto nel `Profilo Utente 360`;
- `Radio`, `WooCommerce dashboard`, `Gamification` e funzioni simili non devono guidare la IA principale del prodotto.

---

## 4. Menu finale consigliato

```text
Home
Utenti
  Ricerca utenti
  Nuovo utente
  Abbonamenti in scadenza
  Utenti bloccati
  Richieste disdetta
  Profilo utente 360

Vendite
  Nuova vendita
  Pagamenti e ricevute
  Rinnovi
  Listino abbonamenti
  Listino pacchetti
  Prodotti e servizi
  Sconti e promo

Accessi
  Monitor live
  Storico accessi
  Check-in desk
  Regole accesso
  Calendari accesso

Attivita
  Calendario corsi
  Prenotazioni servizi
  Risorse e sale
  Istruttori e planning
  Presenze e no-show

Training
  Piani assegnati
  Modelli scheda
  Builder scheda
  Libreria esercizi
  Misure e valutazioni

CRM
  Lead e pipeline
  Campagne e notifiche
  Segmenti
  Task commerciali
  Form e survey

Analytics
  KPI operativi
  Report vendite
  Report accessi
  Report retention
  Export

Impostazioni
  Sedi e orari
  Team e permessi
  Contratti e consensi
  App e branding
  Metodi di pagamento
  Policy accessi
  Integrazioni

Dispositivi (optional)
  Tornelli e varchi
  Docce / phon / servizi controllati
  Comandi manuali
  Log dispositivi
```

---

## 5. Sezioni da costruire davvero

## 5.1 Home

### Perche deve esistere
E la pagina di lavoro quotidiana di reception, manager e operatori.

### Cosa deve mostrare

- accessi di oggi;
- incassi di oggi;
- rinnovi in scadenza;
- certificati in scadenza;
- accessi negati recenti;
- prenotazioni imminenti;
- task urgenti;
- alert dispositivi se il modulo accessi fisici e attivo.

### Da tenere dagli screenshot

- box KPI;
- grafici su accessi e incassi;
- vista rapida di rinnovi e abbonamenti in scadenza.

### Da migliorare rispetto al benchmark

- meno box vuoti e piu task azionabili;
- home diversa per ruolo;
- alert e task prima dei grafici.

### Priorita
`P0`

---

## 5.2 Utenti

### Perche deve esistere
E il cuore del gestionale. Tutto ruota attorno al membro, al lead o al cliente attivo.

### Cosa deve contenere

- ricerca utenti con filtri;
- creazione nuovo utente;
- stati utente: attivo, bloccato, in scadenza, ex cliente;
- onboarding rapido;
- collegamento immediato a vendita, accesso, training, documenti, CRM.

### Dati minimi da gestire bene

- anagrafica;
- contatti;
- data nascita;
- codice fiscale se necessario per il contesto italiano;
- sede principale;
- tag commerciali;
- origine lead;
- istruttore/commerciale assegnato;
- note interne;
- genitore/tutore se utente minorenne.

### Da tenere dagli screenshot

- form anagrafica completa;
- distinzione fra utente attivo, bloccato, presente, in scadenza;
- collegamento a certificati e richieste disdetta.

### Da migliorare rispetto al benchmark

- evitare form troppo lunghi in una sola pagina;
- usare step o sezioni progressive;
- separare dati obbligatori, utili e opzionali.

### Priorita
`P0`

---

## 5.3 Profilo Utente 360

Questa non e una voce separata della sidebar, ma la pagina piu importante dell'intero prodotto. Molte cose viste negli screenshot devono vivere qui, non come moduli top-level separati.

### Tab raccomandate

1. `Panoramica`
2. `Timeline`
3. `Abbonamenti e acquisti`
4. `Pagamenti e ricevute`
5. `Documenti e consensi`
6. `Certificato medico`
7. `Accessi`
8. `Prenotazioni`
9. `Training`
10. `Misure`
11. `CRM e note`

### Quick actions obbligatorie

- nuova vendita;
- rinnovo;
- registra pagamento;
- check-in manuale;
- prenota corso;
- invia notifica;
- sblocca varco se autorizzato.

### Cosa tenere dagli screenshot

- timeline eventi;
- documenti personali e contratti;
- ricevute;
- accessi;
- corsi e prenotazioni;
- anamnesi e pliche;
- piani di allenamento;
- dispositivi;
- tab acquisti / pagamenti / card.

### Cosa NON va copiato 1:1

- troppe tab specialistiche sempre visibili;
- gamification come tab fissa;
- consigli alimentari come area separata se non c'e ancora un prodotto nutrition serio;
- linguaggio troppo tecnico o dispersivo.

### Decisione corretta
Il profilo utente deve essere il posto dove staff e coach lavorano davvero. Se una funzione riguarda una singola persona, prima si valuta qui, non nella sidebar globale.

### Priorita
`P0`

---

## 5.4 Vendite

### Perche deve esistere
Senza un modulo vendite serio, il gestionale non regge l'operativita reale della palestra.

### Ambito corretto del modulo

- nuova vendita;
- rinnovo abbonamento;
- pagamento singolo o rateizzato;
- ricevute;
- rimborsi e storni;
- listino;
- servizi extra;
- pacchetti;
- prodotti;
- sconti e promo;
- crediti prepagati se realmente usati.

### Sottosezioni da prevedere

#### Nuova vendita
- scelta utente;
- scelta tipo vendita;
- calcolo prezzo;
- sconto con policy;
- rate;
- metodo di pagamento;
- emissione ricevuta.

#### Pagamenti e ricevute
- storico;
- stato pagamento;
- ristampa;
- recupero insoluti.

#### Listino
- abbonamenti periodici;
- abbonamenti a ingressi;
- pacchetti;
- servizi;
- prodotti.

### Da tenere dagli screenshot

- flusso "nuova vendita";
- ricarica crediti;
- distinzioni tra abbonamento periodico, ingressi, pacchetto, servizio, prodotto;
- tab acquisti / pagamenti / card.

### Da migliorare rispetto al benchmark

- unificare il linguaggio economico;
- evitare menu troppo frammentati;
- chiarire differenza fra abbonamento, pacchetto, servizio e credito;
- non trattare "card" come concetto a parte se in realta e solo un supporto di accesso o wallet.

### Priorita
`P0`

---

## 5.5 Accessi

### Perche deve esistere
E un modulo core per ogni palestra con tornelli, desk check-in o regole orarie.

### Cosa deve contenere

- monitor live ingressi;
- storico accessi;
- accessi negati con motivazione;
- check-in manuale desk;
- regole di accesso;
- calendari accesso per abbonamento o tipo utente;
- sblocco manuale controllato;
- audit log.

### Da tenere dagli screenshot

- monitor accessi;
- elenco varchi e servizi controllati;
- calendario accessi;
- sblocco tornello con distinzione "non addebitare l'ingresso";
- popup accesso e log accessi consentiti/negati.

### Decisione di prodotto importante
`Accessi` deve coprire sia la logica business sia la logica device:

- logica business: chi puo entrare, quando, con quale titolo;
- logica device: quale tornello, doccia o servizio viene aperto.

Per non sporcare troppo il core:

- la parte business resta dentro `Accessi`;
- la parte hardware avanzata puo andare nel modulo opzionale `Dispositivi`.

### Priorita
`P0`

---

## 5.6 Attivita

### Perche usare questo nome
Negli screenshot compaiono corsi, prenotazioni, campi, planning. Tutto questo va raccolto in un modulo piu chiaro di `Gestione corsi e campi`.

### Cosa deve includere

- calendario corsi;
- calendario servizi prenotabili;
- sale, campi e risorse;
- istruttori assegnati;
- capienze;
- regole di prenotazione;
- cancellazioni e no-show;
- planning giornaliero e settimanale.

### Cosa tenere dagli screenshot

- corsi e prenotazioni;
- calendari settimana;
- planning PT;
- logica di prenotazione con vincoli.

### Decisione corretta
`Planning PT` non deve restare un modulo isolato se serve soltanto a organizzare lezioni, appuntamenti o disponibilita. Va fuso dentro `Attivita` oppure, se strettamente training, richiamato da `Training`.

### Priorita
`P0`

---

## 5.7 Training

### Perche deve esistere
Per Betterfit e uno dei moduli identitari. Non puo essere solo un allegato del gestionale.

### Cosa deve contenere

- modelli scheda;
- builder schede;
- libreria esercizi;
- assegnazione a utente;
- progressi;
- misure e valutazioni;
- export e PDF;
- eventuale storico esecuzione in app.

### Sottosezioni corrette

- `Piani assegnati`
- `Modelli`
- `Builder`
- `Esercizi`
- `Misure e valutazioni`

### Da tenere dagli screenshot

- builder multi-giorno;
- modelli di scheda filtrabili;
- libreria esercizi;
- stampa/preview scheda;
- anamnesi e pliche;
- viste di misurazione.

### Da migliorare rispetto al benchmark

- collegare meglio template, assegnazione e storico;
- semplificare il builder;
- non separare troppo "modello piano", "modello sessione", "training", "scheda" se generano confusione.

### Priorita
`P0`

---

## 5.8 CRM

### Perche deve esistere
Il benchmark mostra moduli CRM, notifiche e form dinamici. Questa area serve davvero, ma va focalizzata sul ciclo commerciale e sulla retention.

### Cosa deve contenere

- lead e pipeline;
- task commerciali;
- segmentazione utenti;
- campagne e notifiche;
- reminder automatici;
- richieste disdetta;
- survey e form;
- storico contatti.

### Da tenere dagli screenshot

- notifiche singole e massive;
- form dinamici;
- domande per disdetta;
- origine lead e tag utente.

### Decisione corretta
`Notifiche` non deve essere una macro-area separata nella IA principale. Deve stare dentro `CRM`, con focus comunicazione e automazioni.

### Priorita
`P1`

---

## 5.9 Analytics

### Perche deve esistere
Serve a manager e owner per capire cosa sta succedendo, non solo per vedere grafici.

### Cosa deve contenere

- KPI operativi;
- accessi;
- incassi;
- rinnovi;
- churn;
- uso dei corsi;
- no-show;
- performance commerciale;
- export.

### Da tenere dagli screenshot

- grafici accessi e incassi;
- cards con utenti attivi, accessi recenti, abbonamenti in scadenza.

### Da migliorare rispetto al benchmark

- meno widget vanity;
- piu filtri per sede, periodo e canale;
- viste per ruolo.

### Priorita
`P1`

---

## 5.10 Impostazioni

### Perche deve esistere
Negli screenshot c'e una grande quantita di configurazioni. Questa area va mantenuta, ma fortemente ordinata.

### Cosa deve contenere

- sedi e dati filiale;
- operatori;
- ruoli e permessi;
- orari e calendari accesso;
- contratti e consensi;
- provider pagamento;
- branding app;
- policy app e prenotazioni;
- configurazioni documenti;
- integrazioni.

### Da tenere dagli screenshot

- configurazione filiale;
- impostazione orari accessi;
- contratti e consensi;
- form dinamici;
- setup app;
- tessere staff;
- monitor accessi e desk;
- configurazioni di stampa ricevute.

### Da migliorare rispetto al benchmark

- dividere impostazioni operative, legali, commerciali e tecniche;
- nascondere settaggi non necessari ai ruoli non tecnici;
- ridurre schermate con molti box indipendenti.

### Priorita
`P0`

---

## 5.11 Dispositivi

### Quando deve esistere come modulo separato
Solo se Betterfit vuole supportare in modo serio hardware e automazioni fisiche.

### Cosa deve contenere

- inventory dispositivi;
- stato online/offline;
- configurazioni controller;
- comandi manuali;
- log hardware;
- regole di fallback;
- audit tecnico.

### Esempi dagli screenshot

- tornelli;
- docce;
- phon;
- monitor accessi;
- selezione desk;
- sblocco manuale per dispositivo specifico.

### Decisione corretta
Se i dispositivi sono solo una piccola estensione della logica accessi, basta `Accessi`.
Se diventano un pillar con controller, heartbeat, comandi manuali e troubleshooting, allora meritano modulo proprio.

### Priorita
`P2 / Optional`

---

## 6. Sezioni che NON devono essere top-level

Questi elementi possono esistere nel prodotto, ma non dovrebbero stare come primo livello della navigazione:

### `Documenti`
Meglio come tab del profilo utente e come configurazione nei template/consensi.

### `Pagamenti`
Meglio come sottosezione di `Vendite`.

### `Certificati`
Meglio come tab del profilo utente, con viste aggregate dentro `Utenti`.

### `Notifiche`
Meglio dentro `CRM`.

### `Planning PT`
Meglio dentro `Attivita` o `Training`.

### `Ricevute e Spese`
Le ricevute stanno in `Vendite`; la parte spese amministrative puo arrivare dopo e non deve guidare la IA principale.

### `Listino`
Meglio dentro `Vendite`.

### `Form dinamici`
Meglio dentro `CRM` o `Impostazioni`, non come modulo primario.

---

## 7. Funzioni da tenere ma con scope ridotto

## 7.1 Nutrizione / consigli alimentari

Utile solo se Betterfit vuole davvero coprire il coaching nutrizionale.

Decisione consigliata:

- non farla diventare sezione top-level adesso;
- inserirla come estensione del profilo utente o del modulo `Training`;
- attivarla solo per ruoli e palestre che la usano davvero.

Priorita: `P2`

## 7.2 Form dinamici

Sono utili per:

- survey di disdetta;
- questionari onboarding;
- anamnesi personalizzate;
- raccolta dati pre-vendita.

Decisione consigliata:

- fare prima un motore semplice di form e risposte;
- rimandare builder molto avanzato.

Priorita: `P1`

## 7.3 Crediti speciali tipo Energy Drop

Negli screenshot si vede una logica molto verticale di crediti dedicati.

Decisione consigliata:

- non costruire un modulo business cucito su un singolo naming o caso d'uso;
- modellare invece un sistema generico di `credit wallet` o `service credits`;
- poi eventualmente configurare use case verticali sopra questo strato.

Priorita: `P1`, ma solo se esiste reale richiesta commerciale.

---

## 8. Funzioni da NON portare nel core iniziale

## 8.1 Radio
Non ha senso come priorita di piattaforma.

## 8.2 Gamification
Interessante, ma non core per la prima struttura del gestionale.

## 8.3 WooCommerce dashboard dedicata
Se servira ecommerce, va integrato dentro `Vendite` o `Integrazioni`, non come widget separato scollegato.

## 8.4 Impostazioni troppo hardware-specifiche subito
Se non c'e un rollout hardware certo, evitare di modellare subito ogni tipo di controller e periferica.

---

## 9. Priorita di rilascio consigliata

## Fase 1 - Core operativo

- Home
- Utenti
- Profilo Utente 360
- Vendite
- Accessi
- Attivita
- Training
- Impostazioni base

Questa fase rende Betterfit usabile in una palestra vera.

## Fase 2 - Crescita gestionale

- CRM completo
- Analytics piu ricchi
- Form dinamici
- wallet crediti generico
- richieste disdetta complete
- policy documenti e consensi piu avanzate

## Fase 3 - Verticalizzazioni e optional

- Dispositivi avanzati
- nutrition strutturata
- gamification
- ecommerce evoluto
- automazioni predittive

---

## 10. Mappatura con il prodotto attuale Betterfit

La direzione gia presente nel repository e buona, perche la dashboard attuale ha gia:

- `Home`
- `Users`
- `Access`
- `Sales`
- `Training`
- `CRM`
- `Reports`
- `Settings`

La parte da aggiungere o rifinire e:

- introdurre `Attivita` come modulo chiaro per corsi, prenotazioni, risorse e planning;
- definire bene il `Profilo Utente 360`;
- inglobare documenti, certificati, pagamenti e notifiche nei moduli giusti;
- separare bene `Accessi` da `Dispositivi` solo quando serve.

---

## 11. Nomi canonici da usare nel prodotto

Per mantenere coerenza tra UI, backend e permessi, questi nomi sono i piu solidi:

| Nome UI | Dominio backend / permessi |
|---|---|
| Utenti | `members` |
| Vendite | `billing`, `plans` |
| Accessi | `checkins` |
| Attivita | `classes` |
| Training | `workouts` |
| Analytics | `reports` |
| Sedi | `locations` |
| Ruoli e permessi | `roles`, `staff_assignments` |
| Security e policy | `security_policy` |

Questo evita che il prodotto cresca con naming disallineato tra frontend, backend e documentazione.

---

## 12. Decisione finale

Le sezioni che Betterfit deve avere davvero sono:

1. `Home`
2. `Utenti`
3. `Vendite`
4. `Accessi`
5. `Attivita`
6. `Training`
7. `CRM`
8. `Analytics`
9. `Impostazioni`

Con due elementi strutturali fondamentali:

- `Profilo Utente 360` come centro operativo vero;
- `Dispositivi` come modulo opzionale e non obbligatorio.

Il benchmark e utile come repertorio funzionale, ma non va copiato come architettura informativa. La direzione giusta per Betterfit e un gestionale meno dispersivo, piu task-oriented e piu coerente con il dominio gia modellato nel backend.
