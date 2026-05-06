# Analyse de sécurité ByteSync

## Trusted clients, sécurité de session et postes sans intervention utilisateur

Date d'analyse : 10 avril 2026

Périmètre analysé :
- `src/ByteSync.Client`
- `src/ByteSync.Common`
- `src/ByteSync.Functions`
- `src/ByteSync.ServerCommon`

## Synthèse exécutive

ByteSync repose sur une identité persistante par machine, matérialisée par une paire de clés RSA et un `ClientId` dérivé de cette identité. Le mécanisme de `trusted clients` est un modèle décentralisé : chaque client conserve localement sa propre liste de clés publiques approuvées, et la première mise en confiance se fait via une comparaison hors bande d'un `SafetyKey`.

La sécurité d'une session cloud repose ensuite sur plusieurs couches successives :
- authentification du client auprès du backend par JWT ;
- contrôle de confiance mutuelle des clés publiques ;
- échange du mot de passe de session via RSA ;
- remise de la clé AES de session via RSA ;
- authentification mutuelle des membres par signatures numériques ;
- chiffrement des métadonnées et des transferts avec la clé AES de session.

Conclusion pratique :
- des postes déjà mutuellement approuvés peuvent fonctionner sans intervention utilisateur ;
- un nouveau poste ne peut pas être enrôlé en zéro-touch avec le même niveau de garantie que le modèle actuel sans ajouter une autre racine de confiance, par exemple une PKI, un MDM, ou un registre de clés signé.

## 1. Comment fonctionne le système de trusted clients

### 1.1. Identité persistante du client

Au premier lancement, ByteSync génère une paire RSA locale puis calcule un `ClientId` stable à partir de :
- `InstallationId`
- la clé publique RSA

Conséquences :
- le `ClientId` représente l'identité durable de la machine dans le modèle de confiance ;
- le `ClientInstanceId` est différent : il est régénéré à chaque instance d'exécution sous la forme `ClientId_GUID`.

En pratique :
- la confiance se fait sur le couple `ClientId` + clé publique RSA ;
- les échanges de session temps réel utilisent surtout `ClientInstanceId`.

### 1.2. Stockage local de la confiance

Les clés de confiance sont stockées localement dans les paramètres applicatifs sous forme de `TrustedPublicKey` :
- `ClientId`
- `PublicKey`
- `PublicKeyHash`
- `SafetyKey`
- `ValidationDate`

Chaque client conserve donc sa propre liste de clients approuvés. Il n'y a pas d'autorité centrale de certification dans le design actuel.

Point important :
- la révocation est locale ;
- supprimer un trusted client sur une machine ne supprime pas automatiquement cette confiance chez les autres membres ;
- une session déjà établie n'est pas cassée rétroactivement, car la clé AES de session a déjà été distribuée.

### 1.3. Protection locale des secrets

Les clés RSA privées, la liste des trusted clients et plusieurs données sensibles sont chiffrées localement dans `ApplicationSettings.xml`.

Le mot de passe de chiffrement local est dérivé d'éléments propres à la machine, dans cet ordre :
- variable d'environnement `BYTESYNC_SYMMETRIC_KEY`
- identifiant machine du système
- valeur de secours statique si rien d'autre n'est disponible

Cela veut dire :
- copier `ApplicationSettings.xml` vers une autre machine ne fonctionne pas en standard ;
- la machine source et la machine cible n'ont normalement pas la même capacité à déchiffrer ces secrets ;
- la persistance des trusted clients dépend donc aussi du contexte local de la machine.

### 1.4. Établissement initial de la confiance

Quand un client veut rejoindre une session, il commence par demander aux membres existants leurs `PublicKeyCheckData`.

Chaque `PublicKeyCheckData` contient essentiellement :
- la clé publique de l'émetteur ;
- son `ClientInstanceId` ;
- la clé publique de l'autre partie ;
- un `Salt` ;
- l'indication de confiance déjà connue par l'autre partie ;
- la version de protocole.

À partir de ces données, ByteSync calcule un `SafetyKey` commun aux deux parties :
- il hache les deux clés publiques ;
- il les trie pour avoir le même ordre des deux côtés ;
- il concatène ces valeurs avec le `Salt` ;
- il calcule enfin un MD5 de cette chaîne.

Ce `SafetyKey` est ensuite affiché sous une forme lisible par l'utilisateur via des mots de sécurité. Les deux utilisateurs doivent comparer cette valeur par un canal indépendant :
- appel téléphonique ;
- visioconférence ;
- messagerie déjà authentifiée ;
- présence physique.

Si les deux côtés valident, la clé publique distante est ajoutée à la liste locale des trusted clients.

### 1.5. Auto-trust si les deux côtés se connaissent déjà

Le point clé pour l'automatisation est ici :
- si le joiner fait déjà confiance au membre ;
- et si le membre fait déjà confiance au joiner ;
- alors ByteSync ne redemande pas de validation humaine.

Le flux de confiance devient alors silencieux :
- les `PublicKeyCheckData` sont échangés ;
- le client constate la confiance mutuelle ;
- le membre est marqué `fully trusted` pour cette session ;
- la session peut continuer sans popup de validation.

C'est précisément ce comportement qui rend possible un fonctionnement headless pour des machines déjà enrôlées.

## 2. Comment sont sécurisées les sessions et quels protocoles sont impliqués

## 2.1. Authentification du client vers le backend

Avant toute opération cloud, le client :
- s'authentifie auprès du backend ;
- reçoit un JWT et un refresh token ;
- conserve ces tokens en mémoire ;
- renouvelle le JWT périodiquement tant que la connexion est active.

Le JWT embarque notamment :
- `ClientId`
- `ClientInstanceId`
- version du client
- plateforme
- adresse IP

Les appels HTTP du backend Azure Functions passent ensuite par un middleware JWT, sauf les endpoints explicitement laissés anonymes comme le login.

Le canal temps réel SignalR utilise aussi l'identité issue de cette phase d'authentification.

En production, la sécurité de transport doit s'appuyer sur HTTPS et WSS. Au-dessus de cette couche, ByteSync ajoute sa propre couche de sécurité applicative pour que le serveur ne soit pas la racine de confiance cryptographique entre pairs.

## 2.2. Séquence complète de jonction d'une session

Le join d'une session cloud suit logiquement quatre phases.

### Phase 1. Contrôle de confiance

Le joiner demande aux membres existants leurs données de contrôle de clé publique :
- le serveur relaie la demande ;
- chaque membre répond avec sa clé publique et son état de confiance vis-à-vis du joiner ;
- le joiner attend la réception de toutes les réponses ;
- un timeout de 30 secondes est appliqué.

Cette phase peut :
- réussir silencieusement si tout le monde est déjà mutuellement trusted ;
- ou ouvrir une validation manuelle si une confiance manque d'un côté ou des deux côtés.

### Phase 2. Échange du mot de passe de session

Une fois la confiance acquise, le joiner demande un `password exchange key` à un membre existant, appelé ici validateur.

Le validateur :
- vérifie qu'il fait confiance à la clé publique du joiner ;
- transmet sa propre clé publique au joiner.

Le joiner :
- construit un objet `ExchangePassword(sessionId, joinerClientInstanceId, sessionPassword)` ;
- chiffre cette donnée avec la clé publique RSA du validateur ;
- l'envoie au backend.

Le backend relaie alors cette preuve chiffrée au validateur, qui :
- déchiffre avec sa clé privée RSA ;
- vérifie que le mot de passe de session reçu correspond au vrai mot de passe de session ;
- s'il est correct, chiffre la clé AES de session avec la clé publique RSA du joiner ;
- envoie cette clé AES chiffrée au joiner.

Propriété importante :
- le mot de passe de session n'est jamais envoyé en clair ;
- la clé AES de session n'est jamais envoyée en clair ;
- le serveur ne voit que des blobs RSA chiffrés.

### Phase 3. Authentification mutuelle par signatures numériques

Le fait d'être trusted ne suffit pas à lui seul. ByteSync ajoute ensuite une authentification active :
- chaque membre signe une chaîne dérivée de la session, de sa propre identité d'instance, de sa clé publique et du destinataire ;
- les autres membres vérifient cette signature avec la clé publique trusted correspondante ;
- le serveur n'autorise pas la finalisation du join tant que les vérifications attendues ne sont pas marquées comme réussies.

La chaîne signée suit l'idée :
- `USERAUTH_REQUEST`
- `sessionId`
- `issuerClientInstanceId`
- `SHA512(publicKey)`
- `recipientClientInstanceId`

Le résultat final est signé en RSA-SHA256.

Effet sécurité :
- prouver qu'un pair possède bien la clé privée associée à la clé publique trusted ;
- éviter qu'un acteur ne se contente de réutiliser une clé publique connue sans posséder la clé privée correspondante ;
- ajouter une authentification mutuelle liée à la session et au destinataire.

### Phase 4. Finalisation du join

Après validation du mot de passe et des signatures :
- le joiner reçoit la clé AES de session ;
- il chiffre ses données privées de membre avec cette clé ;
- il envoie la demande de finalisation ;
- le serveur ne valide définitivement l'entrée dans la session que si tous les contrôles d'authentification attendus sont présents.

## 2.3. Chiffrement des métadonnées de session

Une fois la clé AES de session connue, ByteSync chiffre côté client :
- `SessionSettings`
- `SessionMemberPrivateData`
- `DataNode`
- `DataSource`

Le schéma observé dans le code est :
- AES avec clé de session ;
- IV aléatoire par objet ;
- sérialisation JSON puis chiffrement.

Conséquence :
- le backend stocke et relaie des structures déjà chiffrées ;
- la lecture utile de ces objets nécessite la clé AES de session.

## 2.4. Chiffrement des fichiers

Pour les transferts de fichiers :
- ByteSync prépare un `SharedFileDefinition` avec un IV ;
- découpe le fichier en slices ;
- chiffre les slices avec la clé AES de session ;
- stocke ou télécharge les blobs via Azure Blob Storage ou Cloudflare R2.

Le serveur ne donne les URLs d'upload ou de download qu'à un membre autorisé de la session, et il vérifie que le `ClientInstanceId` propriétaire du fichier partagé correspond bien à un membre de cette session.

Autrement dit :
- l'autorisation d'accès au stockage est contrôlée côté serveur ;
- le contenu lui-même est déjà chiffré côté client avant l'envoi.

## 2.5. Ce que le serveur sait et ne sait pas

Le serveur sait :
- quels clients sont connectés ;
- quels membres appartiennent à quelle session ;
- quelles transitions d'état sont autorisées ;
- quelles structures chiffrées doivent être relayées ou stockées.

Le serveur ne connaît pas en clair :
- le mot de passe de session échangé pendant le join ;
- la clé AES de session ;
- les métadonnées déchiffrées ;
- le contenu utile des fichiers transférés.

Le serveur est donc :
- un orchestrateur ;
- un relais ;
- un point de contrôle d'appartenance ;
- mais pas la source de la confiance cryptographique entre pairs.

## 3. Comment avoir des postes sans intervention utilisateur avec le même niveau de sécurité

## 3.1. Réponse courte

Oui, c'est possible aujourd'hui pour des postes déjà mutuellement trusted.

Non, ce n'est pas possible pour un nouveau poste jamais enrôlé si on veut conserver exactement la même garantie que le modèle actuel sans ajouter une autre racine de confiance.

## 3.2. Cas 1 : postes déjà trusted

C'est le cas le plus favorable, et ByteSync le supporte déjà.

Pourquoi cela fonctionne :
- les trusted clients sont persistés localement ;
- lors d'un nouveau join, la phase de trust check devient silencieuse si la confiance est déjà mutuelle ;
- le client dispose d'un mode headless basé sur ligne de commande ;
- l'exécution automatique d'un profil est prise en charge en `--no-gui`.

Concrètement, le projet contient déjà des arguments prévus pour cela :
- `--join`
- `--inventory`
- `--synchronize`
- `--no-gui`

Dans ce mode, on peut viser l'exploitation suivante :
- enrôlement manuel une seule fois par machine ;
- sauvegarde locale du profil ;
- exécution automatique d'un profil ou d'un lobby cloud ;
- inventaire et synchronisation sans intervention utilisateur.

Préconditions :
- la machine doit déjà posséder sa paire RSA locale ;
- la liste locale de trusted clients doit déjà contenir les autres machines concernées ;
- cette confiance doit être mutuelle ;
- les secrets locaux doivent rester lisibles sur cette machine ;
- le profil et l'orchestration de session doivent déjà exister.

## 3.3. Cas 2 : nouveau poste jamais trusted

Ici, le blocage est structurel et volontaire :
- le niveau de sécurité actuel repose sur une validation hors bande ;
- cette validation sert précisément à empêcher qu'un serveur ou un intermédiaire malveillant injecte une fausse clé publique ;
- si on supprime cette étape sans la remplacer par autre chose, on baisse le niveau de sécurité.

Donc, pour un nouveau poste, il n'existe que deux chemins cohérents :

### Option A. Enrôlement manuel une seule fois, puis zéro-touch

C'est l'option la plus proche du design actuel.

Principe :
- la machine est trustée manuellement une seule fois ;
- ensuite elle peut tourner sans intervention ;
- le niveau de sécurité reste aligné avec le modèle actuel.

Avantages :
- pas besoin de changer l'architecture de sécurité ;
- faible risque ;
- cohérent avec le modèle TOFU actuel.

Inconvénient :
- il faut une première intervention humaine.

### Option B. Ajouter une racine de confiance d'entreprise

Si l'objectif est un vrai zéro-touch pour les nouvelles machines sans baisse de sécurité, il faut remplacer la validation humaine hors bande par une autre source de confiance forte.

Par exemple :
- PKI interne ;
- MDM ;
- annuaire d'équipements signé ;
- certificat machine ;
- attestation matérielle ou d'OS.

Principe recommandé :
1. la nouvelle machine génère localement sa paire RSA ByteSync ;
2. sa clé publique est enregistrée dans un registre d'entreprise signé ;
3. les clients ByteSync n'acceptent l'auto-trust que si la clé publique présentée est validée par cette autorité ;
4. le reste du protocole de session peut rester identique.

Dans ce modèle :
- on garde l'idée "je ne fais confiance qu'à une clé explicitement approuvée" ;
- mais la confiance ne vient plus d'un humain qui compare un SafetyKey ;
- elle vient d'une autorité technique d'entreprise.

### Option C. Pré-provisionnement sécurisé

Une variante consiste à préparer les postes en amont :
- génération locale ou pré-génération de l'identité de la machine ;
- distribution sécurisée de la liste des clés publiques approuvées ;
- injection d'un secret local machine si nécessaire ;
- lancement ensuite en mode headless.

Cette option peut rester solide si, et seulement si :
- chaque machine a sa propre identité RSA ;
- la distribution des clés publiques est sécurisée ;
- la machine n'emprunte pas l'identité cryptographique d'une autre.

## 3.4. Ce qu'il ne faut pas faire

Pour conserver un bon niveau de sécurité, il faut éviter les anti-patterns suivants :

- Copier la même clé privée RSA sur plusieurs machines.
- Utiliser un même `BYTESYNC_SYMMETRIC_KEY` de flotte pour faire vivre la même identité sur plusieurs postes.
- Cloner un `ApplicationSettings.xml` complet d'une machine vers une autre pour simuler un trusted client déjà enrôlé.
- Désactiver la comparaison hors bande sans remplacer cette étape par une autre preuve forte de possession et d'identité.

Pourquoi :
- plusieurs machines deviendraient indiscernables du point de vue de ByteSync ;
- la traçabilité par client serait dégradée ;
- la compromission d'une seule machine compromettrait potentiellement plusieurs identités ;
- on contournerait le principe de confiance explicite sur lequel repose le design actuel.

## 3.5. Recommandation pragmatique

Si l'objectif est opérationnel et réaliste à court terme, la meilleure stratégie est :

1. enrôler chaque nouveau poste une seule fois avec validation humaine ;
2. laisser ensuite tourner le poste en `--no-gui` ;
3. s'appuyer sur des profils ou lobbies déjà préparés ;
4. réserver un projet d'évolution séparé pour un futur auto-enrôlement via PKI ou MDM.

C'est le meilleur compromis entre :
- niveau de sécurité ;
- effort de mise en oeuvre ;
- fidélité au design actuel de ByteSync.

## 4. Points d'attention et durcissements possibles

## 4.1. Choix cryptographiques perfectibles

Le design global est cohérent, mais plusieurs briques observées sont plutôt héritées que modernes :
- RSA avec padding PKCS#1 v1.5 pour le chiffrement ;
- RSA avec signature PKCS#1 v1.5 ;
- AES en mode CBC ;
- dérivation locale de clé avec PBKDF2 et 1000 itérations ;
- `SafetyKey` affiché aux utilisateurs construit à partir d'un MD5.

Cela ne casse pas automatiquement le modèle, mais pour un durcissement futur, il serait préférable d'évoluer vers :
- RSA-OAEP pour le chiffrement ;
- RSA-PSS pour les signatures, ou des primitives de type Ed25519 selon les contraintes ;
- AES-GCM ou ChaCha20-Poly1305 pour bénéficier d'un mode authentifié ;
- une dérivation locale plus robuste ;
- un `SafetyKey` basé sur SHA-256 tronqué plutôt que MD5.

## 4.2. Authenticité des payloads chiffrés

Le code observé chiffre les payloads applicatifs et fichiers avec AES + IV, mais je n'ai pas vu de MAC ou d'AEAD explicite attaché à chaque payload chiffré.

En pratique :
- la confidentialité est bien présente ;
- l'authentification des pairs est bien présente ;
- mais l'intégrité cryptographique de chaque blob chiffré serait plus robuste avec un mode authentifié comme AES-GCM.

## 4.3. Point de vigilance important côté serveur

Le chemin `validateJoin` mérite d'être durci.

Observation :
- l'endpoint Azure Function de validation de join ne reconstruit pas le client authentifié dans la requête de validation ;
- le handler serveur `ValidateJoinCloudSessionCommandHandler` se base sur les identifiants présents dans les paramètres, et non sur l'identité authentifiée de l'appelant ;
- le contrôle métier suivant limite le risque, mais le couplage "appelant authentifié = validateur attendu" n'est pas explicite à cet endroit.

Recommandation :
- transmettre le `client` authentifié jusqu'au handler ;
- vérifier explicitement que `client.ClientInstanceId == ValidatorInstanceId` avant de poursuivre.

Pour une analyse sécurité poussée, c'est le point qui mérite le plus clairement une revue corrective.

## 5. Réponse directe à tes trois questions

### Comment fonctionne le système de client de confiance ?

Chaque machine ByteSync possède une identité RSA persistante. Un client devient trusted quand sa clé publique a été validée hors bande puis stockée localement comme clé approuvée. Ensuite, lors des sessions suivantes, cette confiance est réutilisée automatiquement tant que la clé publique présentée correspond exactement à la clé déjà enregistrée.

### Comment sont sécurisées les sessions et quels protocoles sont impliqués ?

Les sessions sont protégées par :
- JWT pour l'accès au backend ;
- SignalR pour les échanges temps réel ;
- RSA pour établir la confiance initiale, échanger le mot de passe de session et remettre la clé AES ;
- signatures numériques RSA-SHA256 pour authentifier activement les membres ;
- AES pour chiffrer les métadonnées de session et les fichiers.

Le serveur orchestre et relaie, mais la confiance cryptographique et le déchiffrement utiles restent côté clients.

### Comment avoir des postes sans intervention utilisateur avec le même niveau de sécurité ?

Pour des postes déjà trusted, c'est possible dès maintenant en mode headless.

Pour un nouveau poste, ce n'est pas possible sans intervention humaine tant qu'on reste dans le modèle actuel. Pour conserver le même niveau de sécurité sans intervention, il faut ajouter une autre racine de confiance, par exemple une PKI ou un MDM, afin de remplacer la validation humaine du `SafetyKey`.
