# BetterFit - Architettura High-Level, Onboarding e Privacy

## 1. Scopo del documento
Questo documento definisce una proposta architetturale high-level per BetterFit con focus su:

- onboarding di palestre e clienti;
- relazione tra account piattaforma e rapporto con la singola palestra;
- gestione dei dati personali, fiscali e sanitari;
- portabilita e migrazione fra palestre;
- principi organizzativi e tecnici utili per lavorare in ottica GDPR.

Obiettivo: sbloccare le decisioni di prodotto e di architettura prima che il codice evolva in direzioni difficili da correggere.

Nota importante: questo documento non sostituisce un parere legale. Serve come base operativa e di prodotto da validare con un consulente privacy prima del go-live, specialmente se BetterFit trattera dati sanitari su scala ampia.

---

## 2. Executive summary

### Decisione raccomandata
BetterFit dovrebbe adottare un modello ibrido:

1. la palestra si registra e crea il proprio tenant;
2. la palestra puo invitare o pre-registrare i clienti;
3. il cliente puo anche registrarsi in autonomia e collegarsi a una palestra tramite invito, codice o link temporaneo;
4. l'identita digitale della persona deve essere separata dal rapporto con la palestra;
5. i dati non devono essere condivisi automaticamente fra palestre diverse.

### Conseguenze pratiche
- Un utente puo avere un solo account BetterFit e piu relazioni con una o piu palestre nel tempo.
- Ogni palestra vede solo i dati necessari per il proprio rapporto contrattuale e operativo.
- Il passaggio a un'altra palestra deve avvenire tramite portabilita o import su iniziativa dell'utente, non tramite "riuso silenzioso" del database fra tenant.
- I dati salute e i documenti ad alto rischio non dovrebbero entrare nel primo disegno cross-gym.

---

## 3. Modello operativo raccomandato

### 3.1 Separare identita e membership
Il modello corretto non e:
- "la palestra possiede totalmente l'account del cliente"; oppure
- "il cliente esiste solo come utente globale e tutte le palestre leggono tutto".

Il modello raccomandato e:

- `PersonAccount`: identita globale BetterFit della persona.
- `GymTenant`: singola palestra o gruppo palestre.
- `GymMembership`: relazione tra persona e palestra.
- `GymRole`: ruolo nel tenant (`member`, `coach`, `reception`, `manager`, `owner`).
- `ClubProfileData`: dati che servono a quella palestra per operare.
- `PlatformAccountData`: dati minimi di autenticazione, sicurezza, preferenze base.

### 3.2 Chi crea l'account?
La risposta migliore e: entrambi, ma in modo governato.

#### Opzione raccomandata
- La palestra puo creare un profilo "invitato" o "bozza membro".
- Il cliente riceve un link/codice e completa il claim del profilo.
- Se il cliente si registra prima, puo collegarsi alla palestra tramite invito o codice palestra.
- Se esiste gia un account con stessa email/telefono, BetterFit propone il collegamento invece di creare un duplicato.

Questo approccio evita due estremi:
- dipendere sempre dalla reception;
- lasciare accessi casuali a qualunque palestra solo conoscendo un codice generico.

### 3.3 Modalita di collegamento consigliata
Usare un `invitation token` o `claim token` con:

- scadenza breve;
- scope limitato a una palestra;
- possibile uso una tantum;
- audit log;
- eventuale approvazione manuale se i dati non coincidono.

Evitare codici permanenti semplici del tipo `GYM123`, utili per marketing ma deboli per collegare persone a una struttura che tratta dati personali.

---

## 4. Flussi principali

## 4.1 Onboarding palestra
1. La palestra crea il tenant BetterFit.
2. Inserisce dati societari, sedi, admin iniziale e configurazioni base.
3. Accetta i documenti contrattuali con BetterFit.
4. Viene definito il perimetro privacy:
   - chi e titolare;
   - chi e responsabile;
   - eventuali sub-responsabili;
   - sedi di hosting;
   - tempi di conservazione;
   - canali di supporto e accesso tecnico.
5. Vengono creati ruoli e permessi.
6. Vengono attivate policy di retention, audit e sicurezza.

## 4.2 Nuovo cliente invitato dalla palestra
1. La reception crea un lead o una bozza membro.
2. Il sistema genera un invito sicuro.
3. Il cliente apre il link, verifica identita e completa il profilo.
4. Il sistema crea o collega il `PersonAccount`.
5. Viene creata la `GymMembership`.
6. Il cliente fornisce solo i dati necessari per il caso d'uso effettivo.
7. La palestra completa eventuali passaggi amministrativi: abbonamento, contratto, pagamenti, documenti.

## 4.3 Cliente che si registra in autonomia
1. Il cliente crea un account BetterFit con dati minimi.
2. Cerca o seleziona la palestra.
3. Inserisce invito/codice/link.
4. La palestra riceve richiesta di collegamento oppure il sistema la approva automaticamente se il token e valido.
5. Solo dopo il collegamento vengono richiesti i dati specifici necessari alla palestra.

## 4.4 Reception-assisted onboarding
Utile in palestra fisica:

1. operatore apre "Nuovo membro";
2. raccoglie dati minimi;
3. crea membership in stato `pending-claim`;
4. conclude il pagamento o l'attivazione;
5. invia al cliente il link per attivare l'account personale.

Questo flusso e ottimo per non bloccare la vendita alla reception, ma preserva il fatto che l'account finale resti sotto controllo dell'utente.

## 4.5 Cambio palestra
Flusso raccomandato:

1. il cliente si collega alla nuova palestra;
2. la nuova palestra crea una nuova `GymMembership`;
3. il cliente puo importare dati selezionati dal proprio archivio o da export precedente;
4. la vecchia palestra conserva o cancella i dati secondo obblighi propri e tempi di retention propri.

Il passaggio non deve implicare che la palestra B erediti automaticamente tutto il fascicolo della palestra A.

---

## 5. Scelta architetturale chiave: tenant isolation

BetterFit dovrebbe essere progettata come piattaforma multi-tenant con isolamento forte:

- dati palestra separati logicamente per tenant;
- autorizzazioni sempre `tenant-scoped`;
- audit log per operazioni critiche;
- accessi di supporto tecnico tracciati e temporanei;
- export/import governati da policy;
- nessuna query applicativa cross-tenant senza motivo documentato.

### Entita concettuali consigliate
- `person_accounts`
- `account_identities`
- `gym_tenants`
- `gym_memberships`
- `membership_status_history`
- `member_profiles`
- `contracts`
- `consents`
- `payments`
- `invoices`
- `bookings`
- `workout_plans`
- `measurements`
- `health_documents`
- `access_events`
- `audit_events`
- `data_portability_requests`

### Regola d'oro
L'account BetterFit e globale. Tutto cio che riguarda il rapporto con la palestra e locale al tenant, salvo cio che l'utente esporta o riutilizza esplicitamente.

---

## 6. Ruoli privacy raccomandati

## 6.1 Modello base consigliato
Nella maggior parte dei casi:

- la palestra e `titolare del trattamento` per i dati dei propri clienti gestiti nel gestionale;
- BetterFit e `responsabile del trattamento` per l'erogazione del software e dei servizi cloud collegati;
- BetterFit puo essere `titolare autonomo` solo per trattamenti propri come:
  - account di autenticazione piattaforma;
  - sicurezza, antifrode, logging tecnico;
  - fatturazione verso la palestra;
  - supporto e compliance interna.

### Perche questa impostazione e la piu solida
Secondo la logica GDPR, il titolare decide finalita e mezzi essenziali del trattamento; il responsabile tratta dati per conto del titolare e con contratto adeguato. Per un SaaS gestionale, il modello piu pulito e che la palestra resti titolare dei dati dei membri e la piattaforma operi principalmente come responsabile.

### Cosa evitare
Evitare, salvo casi realmente necessari, di impostare BetterFit e palestra come contitolari per tutto: complica informative, responsabilita, esercizio dei diritti e flussi di migrazione.

## 6.2 Contratti e documenti minimi
Da prevedere:

- DPA/accordo ex art. 28 tra BetterFit e palestra;
- elenco sub-responsabili;
- informativa piattaforma per i trattamenti di BetterFit da titolare;
- informativa palestra per i trattamenti di membership;
- registro trattamenti;
- policy data breach;
- policy di accesso tecnico e supporto remoto;
- policy di cancellazione/restituzione dati a fine contratto.

---

## 7. Dati da raccogliere: cosa serve davvero

## 7.1 Dati minimi per account BetterFit
Per l'account piattaforma servono solo dati minimi, ad esempio:

- email oppure telefono;
- password o login federato;
- lingua e preferenze essenziali;
- log di sicurezza.

Il codice fiscale non dovrebbe essere richiesto alla registrazione generale della piattaforma.

## 7.2 Dati per membership con la palestra
Da raccogliere quando davvero necessari:

- nome e cognome;
- data di nascita;
- contatti;
- dati amministrativi di abbonamento;
- eventuali dati fiscali per ricevute/fatture;
- stato consensi;
- stato documenti obbligatori.

## 7.3 Codice fiscale
Inferenza giuridica ragionevole: il codice fiscale va trattato come dato personale identificativo/fiscale, ma non va equiparato automaticamente ai dati particolari dell'art. 9 GDPR. La criticita sale quando lo combini con dati salute, pagamenti, documenti o profilazione.

Decisione di prodotto raccomandata:
- non raccoglierlo nel signup;
- richiederlo solo quando serve per finalita fiscali, contrattuali o amministrative;
- mostrarlo solo a ruoli autorizzati;
- mascherarlo in UI dove possibile;
- cifrarlo at-rest o proteggerlo con segregazione forte.

## 7.4 Dati salute
Per BetterFit il vero punto delicato non e il codice fiscale ma i dati salute.

Esempi tipici nel dominio fitness:
- certificato medico;
- anamnesi o limitazioni fisiche;
- informazioni su infortuni;
- note su patologie;
- dati biometrici o body composition se collegabili alla persona.

Questi dati ricadono piu facilmente nelle categorie particolari e richiedono maggiore cautela.

### Regola di prodotto raccomandata
Se non servono, non raccoglierli.

### Se servono davvero
- separare i permessi di accesso;
- raccogliere il minimo indispensabile;
- evitare copie inutili;
- distinguere `stato documento valido/scaduto` dal `documento integrale`;
- registrare chi accede;
- definire retention specifica;
- prevedere DPIA se il trattamento diventa ampio o particolarmente invasivo.

---

## 8. Migrazione e portabilita: cosa e fattibile davvero

## 8.1 Portabilita si, ma user-driven
Il GDPR riconosce il diritto alla portabilita dei dati in determinati casi, in particolare quando il trattamento e automatizzato e si basa su consenso o contratto. Inoltre, quando tecnicamente fattibile, l'interessato puo chiedere la trasmissione diretta a un altro titolare.

Per BetterFit questo significa:

- si puo prevedere un export strutturato dei dati personali del cliente;
- si puo prevedere un import nella nuova palestra;
- si puo prevedere, come fase avanzata, una trasmissione diretta tra titolari su richiesta esplicita dell'utente.

## 8.2 Cosa non fare
Non e raccomandabile impostare un meccanismo in cui:

- appena un utente entra in una nuova palestra;
- BetterFit rende automaticamente disponibili tutti i dati storici della palestra precedente.

Questo sarebbe fragile sotto tre profili:
- finalita diverse tra titolari diversi;
- minimizzazione;
- dati sensibili o documenti che la nuova palestra potrebbe non avere titolo a vedere subito.

## 8.3 Modello raccomandato di migrazione

### Fase 1
Supportare:
- export self-service;
- import guidato;
- scelta per categorie di dati;
- nuovo consenso o nuova presa visione dove necessario.

### Fase 2
Valutare:
- trasferimento diretto tra palestre solo su richiesta forte dell'utente;
- mapping campi standardizzato;
- log della richiesta e della consegna;
- esclusione predefinita dei documenti salute salvo scelta esplicita e base giuridica adeguata.

### Fase 3 opzionale
Valutare un `personal locker` sotto controllo dell'utente, separato dai tenant palestra.

Questa opzione e interessante ma va trattata come prodotto autonomo, perche cambia il ruolo privacy di BetterFit e puo far crescere molto il rischio regolatorio.

Conclusione pratica: la migrazione e fattibile, ma la forma sicura per partire non e la sincronizzazione automatica cross-gym; e la portabilita selettiva guidata dall'utente.

---

## 9. Decisioni di privacy by design per il prodotto

## 9.1 Scelte consigliate subito
- account globale minimo;
- dati palestra tenant-scoped;
- claim via invito/link/token;
- niente condivisione automatica tra palestre;
- niente biometria nel MVP;
- niente dati salute nel profilo globale;
- export/import come unica funzione di riuso iniziale;
- permessi granulari per ruolo;
- audit forte su accessi, override, export, download documenti.

## 9.2 Scelte di default raccomandate
- campi facoltativi non preselezionati;
- consensi marketing separati dal contratto;
- visibilita dei dati limitata per ruolo;
- retention piu breve possibile;
- documenti non accessibili oltre il tenant di appartenenza;
- supporto tecnico con accesso temporaneo e registrato.

---

## 10. Sicurezza e compliance operative

## 10.1 Misure minime da prevedere
- cifratura in transito e at-rest;
- segregazione tenant;
- RBAC per ruolo e sede;
- MFA per staff palestra e admin BetterFit;
- password hashing robusto;
- secret management centralizzato;
- logging e audit immutabili o comunque anti-tamper;
- backup cifrati e test di restore;
- accesso di supporto tramite sessioni approvate e tracciate;
- minimizzazione dei dump e dei dati negli ambienti di test;
- pseudonimizzazione dove possibile.

## 10.2 Incident e breach management
Serve un processo scritto che distingua:

- incidente tecnico;
- sospetta data breach;
- data breach confermata.

BetterFit, se agisce come responsabile, deve poter notificare rapidamente la palestra titolare in caso di violazione. Va quindi progettato un flusso operativo interno con rilevazione, triage, raccolta evidenze, comunicazione e audit.

## 10.3 Trasferimenti extra UE
Se usate servizi fuori SEE o fornitori con accesso da Paesi terzi, va verificata la base di trasferimento appropriata, ad esempio decisione di adeguatezza o SCC dove necessario. Questo punto va deciso insieme alla scelta dei provider.

---

## 11. DPIA, DPO e soglie di attenzione

## 11.1 Quando BetterFit deve alzare il livello
Va previsto un assessment dedicato se il prodotto include:

- trattamento su larga scala di dati salute;
- monitoraggio sistematico su larga scala;
- profilazione significativa;
- biometria;
- integrazione wearables con dati sanitari;
- scoring automatici che incidono in modo rilevante sugli utenti.

## 11.2 Lettura pragmatica per BetterFit
Se il primo perimetro e:

- account;
- prenotazioni;
- pagamenti;
- accessi;
- documenti minimi;

allora il rischio resta gestibile, pur con adempimenti seri.

Se invece il perimetro diventa:

- certificati medici completi;
- informazioni sanitarie ricche;
- misurazioni sensibili;
- wearable data;
- analisi predittive su salute o comportamento;

allora va impostata una DPIA vera prima del rilascio.

Sul DPO: la nomina non dipende dalla dimensione dell'azienda ma dalla natura e scala dei trattamenti. Se BetterFit o le palestre trattano dati particolari su larga scala o fanno monitoraggio regolare e sistematico su larga scala, la nomina puo diventare necessaria.

---

## 12. Proposta di data taxonomy

| Categoria | Esempi | Scope | Rischio | Default |
|---|---|---|---|---|
| Account platform | email, telefono, password hash, lingua | globale | medio | raccogliere minimo |
| Membership palestra | anagrafica, stato abbonamento, prenotazioni | tenant | medio | tenant-scoped |
| Fiscale/amministrativo | codice fiscale, fatture, ricevute | tenant | medio-alto | solo se necessario |
| Training/fitness | schede, progressi, misure | tenant | medio-alto | opt-in funzionale |
| Salute/documenti | certificati, limitazioni, note mediche | tenant | alto | minimizzare, accesso ristretto |
| Log e audit | accessi, eventi, comandi, export | misto | medio | obbligatori per accountability |

---

## 13. Flusso consigliato per il MVP

### MVP sicuro e realistico
1. Tenant palestra.
2. Admin palestra.
3. Staff con ruoli.
4. Cliente invitato o auto-registrato.
5. Link via token alla palestra.
6. Raccolta dati minima iniziale.
7. Codice fiscale solo se richiesto dal processo amministrativo.
8. Certificati e dati salute solo in modulo dedicato e con accesso ristretto.
9. Export dati da area utente.
10. Import dati in nuova palestra solo su azione dell'utente.

### Cosa rinviare a una fase successiva
- personal locker cross-gym;
- trasferimento diretto dati fra palestre;
- biometria;
- wearable integrations;
- analytics predittive su salute o comportamento;
- automazioni di marketing avanzate su dati sensibili.

---

## 14. Decisioni di prodotto consigliate

## 14.1 Risposta al dubbio iniziale
Alla domanda:

> quando viene registrata una palestra, come vengono poi registrati i clienti?

La risposta consigliata e:

- la palestra puo avviare la registrazione;
- il cliente deve poter completare o rivendicare il proprio account;
- il collegamento deve avvenire tramite invito o token sicuro;
- il sistema deve evitare duplicati e separare identita piattaforma da dati tenant.

## 14.2 Risposta al tema migrazione
Alla domanda:

> l'utente puo migrare a un'altra palestra BetterFit mantenendo gli stessi dati?

La risposta consigliata e:

- si, ma in forma di portabilita guidata dall'utente;
- non come condivisione automatica completa tra palestre;
- con esclusione predefinita dei dati piu delicati;
- con base giuridica, log e controlli chiari.

## 14.3 Posizione consigliata per partire
Per la prima versione:

- account unico BetterFit;
- membership separate per palestra;
- dati salute solo locali al tenant;
- migrazione tramite export/import;
- nessun riuso automatico cross-tenant.

Questa e la configurazione piu difendibile sia tecnicamente sia lato privacy.

---

## 15. Checklist operativa pre-release
- definire data model account vs membership vs tenant;
- scrivere DPA standard BetterFit-palestra;
- censire sub-processors e hosting;
- definire retention per categoria dati;
- definire ruoli e permessi con matrice accessi;
- progettare audit log e accesso supporto;
- progettare export/import dati utente;
- separare consensi marketing da consensi contrattuali;
- decidere se il modulo salute entra nel MVP o no;
- eseguire privacy review e, se necessario, DPIA;
- predisporre runbook data breach;
- predisporre informative distinte piattaforma/palestra.

---

## 16. Fonti ufficiali consultate
- GDPR su EUR-Lex: https://eur-lex.europa.eu/legal-content/en/ALL/?uri=CELEX%3A02016R0679-20160504
- Commissione europea, ruoli titolare/responsabile: https://commission.europa.eu/law/law-topic/data-protection/rules-business-and-organisations/obligations/controllerprocessor/what-data-controller-or-data-processor_it
- Commissione europea, trattamento per conto del titolare e contratto con il responsabile: https://commission.europa.eu/law/law-topic/data-protection/rules-business-and-organisations/obligations/controllerprocessor/can-someone-else-process-data-my-organisations-behalf_it
- Commissione europea, privacy by design e by default: https://commission.europa.eu/law/law-topic/data-protection/rules-business-and-organisations/obligations/what-does-data-protection-design-and-default-mean_it
- Commissione europea, minimizzazione: https://commission.europa.eu/law/law-topic/data-protection/rules-business-and-organisations/principles-gdpr/how-much-data-can-be-collected_en
- Commissione europea, conservazione dei dati: https://commission.europa.eu/law/law-topic/data-protection/rules-business-and-organisations/principles-gdpr/how-long-can-data-be-kept-and-it-necessary-update-it_en
- Commissione europea, diritti dell'interessato e portabilita: https://commission.europa.eu/law/law-topic/data-protection/information-individuals_en
- Commissione europea, DPIA: https://commission.europa.eu/law/law-topic/data-protection/rules-business-and-organisations/obligations/when-data-protection-impact-assessment-dpia-required_en
- Commissione europea, data breach: https://commission.europa.eu/law/law-topic/data-protection/rules-business-and-organisations/obligations/what-data-breach-and-what-do-we-have-do-case-data-breach_it
- Commissione europea, DPO: https://commission.europa.eu/law/law-topic/data-protection/rules-business-and-organisations/obligations/data-protection-officers/does-my-companyorganisation-need-have-data-protection-officer-dpo_en
- EDPB SME guide, secure personal data: https://www.edpb.europa.eu/sme-data-protection-guide/secure-personal-data_en
- Garante Privacy, dati relativi alla salute come categorie particolari e divieto di diffusione: https://gpdp.it/web/guest/home/docweb/-/docweb-display/print/9830919
- Garante Privacy, compendio sulle piattaforme che trattano dati anche relativi alla salute: https://www.gpdp.it/documents/10160/0/Compendio%2Bsul%2Btrattamento%2Bdei%2Bdati%2Bpersonali%2Battraverso%2Bpiattaforme%2Bvolte%2Ba%2Bmettere%2Bin%2Bcontatto%2Bi%2Bpazienti%2Bcon%2Bi%2Bprofessionisti%2Bsanitari%2Baccessibili%2Bvia%2Bweb%2Be%2Bapp.pdf/7fc9ca53-f078-af9b-248d-a71dee74da07?download=true&version=2.0
- Garante Privacy, codice di condotta per software gestionali: https://www.gpdp.it/documents/10160/0/Codice%2Bdi%2Bcondotta%2Bper%2Bil%2Btrattamento%2Bdei%2Bdati%2Bpersonali%2Beffettuato%2Bdalle%2Bimprese%2Bdi%2Bsviluppo%2Be%2Bproduzione%2Bdi%2Bsoftware%2Bgestionale.pdf/a89ad70a-ee18-22fa-8e01-578b04f1023e?version=2.0

