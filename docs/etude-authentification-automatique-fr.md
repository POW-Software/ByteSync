# Etude technique ByteSync

## Authentification et connexion automatique entre machines de confiance

Date : 5 mai 2026

Périmètre analysé :
- `src/ByteSync.Client`
- `src/ByteSync.Common`
- `src/ByteSync.Functions`
- `src/ByteSync.ServerCommon`

## 1. Contexte de l'étude

Dans le cadre de cette étude, l'objectif est d'analyser comment ByteSync authentifie aujourd'hui ses clients et ses sessions, puis d'évaluer la faisabilité d'une connexion automatisée entre deux machines sans intervention manuelle.

Le besoin formulé est le suivant :
- ne plus saisir manuellement l'identifiant de session ;
- ne plus saisir manuellement le mot de passe de session ;
- permettre à deux machines déjà reconnues comme fiables de se connecter automatiquement ;
- conserver un niveau de sécurité cohérent avec l'architecture existante de ByteSync.

Le sujet doit donc être abordé en distinguant deux niveaux d'authentification :
- l'authentification du client auprès du backend ByteSync ;
- l'authentification de confiance entre les machines qui participent à une session.

## 2. Synthèse exécutive

ByteSync possède déjà une base technique favorable à une connexion sans mot de passe entre machines de confiance.

En effet, le logiciel repose déjà sur :
- une identité cryptographique persistante par machine, via une paire de clés RSA ;
- un `ClientId` dérivé de cette identité ;
- une liste locale de clients de confiance ;
- une validation mutuelle des clés publiques ;
- un échange protégé du mot de passe de session ;
- une authentification complémentaire par signatures numériques.

Conclusion principale :
- une connexion automatisée entre deux machines déjà appairées est faisable ;
- en revanche, un enrôlement initial totalement sans interaction humaine n'est pas recommandé sans ajouter une nouvelle racine de confiance ;
- le meilleur modèle pour ByteSync est un mécanisme de `trusted devices` persistants fondé sur les clés publiques déjà existantes, complété par un challenge signé et une autorisation de reconnexion automatique.

## 3. Fonctionnement actuel de l'authentification dans ByteSync

## 3.1. Authentification client vers serveur

Avant de créer ou rejoindre une session, le client doit d'abord s'authentifier auprès du backend Azure Functions.

Le flux est le suivant :
- le client appelle `auth/login` ;
- il transmet son `ClientId`, son `ClientInstanceId`, sa version et sa plateforme ;
- le serveur vérifie la validité minimale de ces informations ;
- s'il accepte la connexion, il émet un JWT et un refresh token ;
- le client ouvre ensuite la connexion SignalR en réutilisant ce jeton.

Ce mécanisme permet au backend d'identifier l'instance cliente active.

Il faut bien noter que :
- le `ClientId` représente l'identité stable de la machine ;
- le `ClientInstanceId` représente une instance d'exécution du client ;
- le JWT protège la relation client vers serveur, mais ne suffit pas à lui seul à établir la confiance entre machines.

En pratique, ce premier niveau répond à la question : "qui parle au backend ?"

## 3.2. Création d'une session cloud

Lorsqu'un poste crée une session :
- ByteSync génère les paramètres de session ;
- chiffre les données sensibles de session ;
- enregistre le créateur comme premier membre ;
- associe à cette session la clé publique du créateur ;
- lui attribue un identifiant de session généré côté serveur.

Le créateur devient alors la première référence de confiance dans la session.

## 3.3. Rejoindre une session

Aujourd'hui, pour rejoindre une session cloud, l'utilisateur doit connaître :
- l'identifiant de session ;
- le mot de passe de session.

Le join ne consiste cependant pas à envoyer simplement ce mot de passe au serveur.

Le processus réel comporte plusieurs étapes de sécurité :

### Etape 1. Contrôle de confiance des clés publiques

Le client qui veut rejoindre la session demande aux membres déjà présents leurs informations de clé publique.

Chaque membre renvoie des `PublicKeyCheckData`, contenant notamment :
- sa clé publique ;
- son identité cliente ;
- un `Salt` ;
- l'indication de confiance déjà connue ;
- la version de protocole.

Le joiner collecte ces réponses et détermine s'il fait déjà confiance aux membres de la session et si ceux-ci lui font déjà confiance.

Deux cas sont possibles :
- soit les deux côtés se connaissent déjà et la vérification est silencieuse ;
- soit une validation humaine est requise pour ajouter un nouveau client de confiance.

### Etape 2. Validation humaine lors du premier appairage

Si les deux machines ne sont pas encore mutuellement approuvées, ByteSync ouvre un processus de validation manuelle.

Les deux côtés comparent alors une empreinte commune, appelée `SafetyKey`, dérivée des deux clés publiques et d'un sel partagé.

Cette étape sert à éviter une attaque de type homme du milieu lors de la première mise en confiance.

Autrement dit :
- si la comparaison visuelle réussit, la clé publique distante est enregistrée comme fiable ;
- si elle échoue, la connexion doit être refusée.

### Etape 3. Echange protégé du mot de passe de session

Une fois la confiance établie, le mot de passe de session n'est pas transmis en clair.

Le déroulement est le suivant :
- le joiner demande une clé d'échange au validateur ;
- le validateur transmet sa clé publique au joiner ;
- le joiner chiffre le mot de passe de session avec cette clé publique ;
- le backend relaie ce mot de passe chiffré au validateur ;
- le validateur le déchiffre localement avec sa clé privée ;
- s'il est correct, il chiffre la clé AES de session pour le joiner.

Ainsi :
- le serveur ne voit pas le mot de passe en clair ;
- le serveur ne voit pas la clé AES en clair ;
- le backend joue un rôle de relais, mais n'est pas la racine de confiance cryptographique entre clients.

### Etape 4. Authentification mutuelle par signatures numériques

ByteSync ajoute ensuite une vérification active.

Chaque membre doit prouver qu'il possède bien la clé privée correspondant à la clé publique annoncée.

Cette preuve repose sur un mécanisme de signatures numériques :
- un membre signe une donnée liée à la session et au destinataire ;
- l'autre membre vérifie la signature avec la clé publique déjà approuvée ;
- le serveur n'autorise la finalisation du join que lorsque les contrôles attendus sont validés.

Cette couche empêche qu'un attaquant se contente de rejouer une clé publique connue sans posséder réellement la clé privée associée.

### Etape 5. Finalisation du join

Une fois le mot de passe validé et les signatures vérifiées :
- le joiner reçoit la clé AES de session ;
- il chiffre ses données privées de membre ;
- il finalise son inscription dans la session ;
- le serveur le bascule du statut de pré-membre à membre actif.

## 4. Ce que cela signifie du point de vue sécurité

Le modèle actuel de ByteSync est déjà plus robuste qu'un simple système "session + mot de passe".

Il repose en réalité sur plusieurs couches :
- authentification applicative au backend par JWT ;
- contrôle de confiance sur les clés publiques ;
- échange RSA du mot de passe de session ;
- échange RSA de la clé AES de session ;
- authentification mutuelle par signatures ;
- chiffrement symétrique des données de session.

Il faut donc retenir que le mot de passe de session n'est qu'un élément parmi d'autres.

La vraie base de confiance durable est la paire de clés RSA locale et la liste de `trusted clients`.

## 5. Faisabilité d'une connexion automatisée sans saisie manuelle

## 5.1. Réponse courte

Oui, c'est faisable pour des machines déjà connues comme fiables.

Non, il n'est pas souhaitable d'automatiser complètement l'enrôlement initial d'une nouvelle machine sans ajouter une autre source de confiance.

## 5.2. Pourquoi c'est faisable

ByteSync dispose déjà des briques nécessaires :
- une identité cryptographique persistante par machine ;
- un mécanisme de vérification de la clé publique distante ;
- un stockage local des clients approuvés ;
- un échange chiffré de la clé de session ;
- une preuve de possession de clé privée via signatures.

En conséquence, si deux machines ont déjà été appairées une première fois manuellement, on peut envisager un mode où :
- elles se reconnaissent automatiquement ;
- elles prouvent automatiquement leur identité ;
- elles s'autorisent automatiquement à rejoindre une session sans que l'utilisateur recopie un identifiant ou un mot de passe.

## 6. Modèle recommandé pour les machines de confiance

## 6.1. Principe général

Le modèle recommandé est le suivant :

### Phase 1. Appairage initial manuel

Lors de la première rencontre entre deux machines :
- les utilisateurs comparent les informations de sécurité ;
- les clés publiques sont ajoutées à la liste des trusted clients ;
- une relation de confiance durable est créée.

Cette phase reste manuelle car elle constitue la racine de confiance.

### Phase 2. Enregistrement d'une autorisation persistante

Après validation initiale, la machine hôte peut mémoriser qu'un autre client a le droit de rejoindre automatiquement certaines sessions ou certains profils.

Cette autorisation peut être modélisée comme un `TrustedDeviceGrant` contenant par exemple :
- `OwnerClientId` ;
- `TrustedClientId` ;
- empreinte de clé publique ;
- date de création ;
- date d'expiration ;
- état de révocation ;
- portée de l'autorisation.

La portée peut être :
- globale à la machine ;
- limitée à un profil ;
- limitée à une session cloud ;
- limitée à certaines plages horaires ou règles locales.

### Phase 3. Reconnexion automatique

Lors d'une future tentative de connexion :
- le joiner n'entre ni session ni mot de passe ;
- il présente son identité cryptographique au serveur ;
- le serveur vérifie qu'une autorisation persistante existe ;
- l'hôte valide automatiquement la demande si la policy locale l'autorise ;
- la clé AES de session est ensuite remise au joiner via chiffrement asymétrique comme aujourd'hui.

Ce modèle conserve l'architecture de sécurité existante tout en supprimant l'interaction humaine répétitive.

## 6.2. Mécanisme d'authentification recommandé

Le mécanisme le plus robuste pour l'auto-connexion est un challenge-réponse signé.

Le flux recommandé serait :

1. le joiner demande à rejoindre la machine ou le profil cible ;
2. le serveur ou l'hôte génère un nonce aléatoire ;
3. le joiner signe ce nonce avec sa clé privée locale ;
4. le serveur ou l'hôte vérifie la signature avec la clé publique déjà approuvée ;
5. si la signature est valide et que le device est autorisé, la connexion est acceptée automatiquement.

Ce mécanisme prouve non seulement l'identité déclarée, mais aussi la possession réelle de la clé privée.

## 6.3. Pourquoi ce modèle est préférable à un simple token persistant

Un simple token persistant améliore l'ergonomie, mais il peut être volé puis rejoué.

A l'inverse, une preuve par signature asymétrique :
- lie l'authentification à la clé privée du device ;
- réduit le risque de rejeu passif ;
- s'intègre naturellement à l'architecture ByteSync existante ;
- évite de remplacer la confiance cryptographique par un simple secret stocké.

Le bon compromis est donc :
- une autorisation persistante de trusted device ;
- plus un challenge signé à chaque reconnexion.

## 7. Solutions techniques envisageables

## 7.1. Solution A. Réutiliser le système actuel de trusted clients avec auto-accept

Principe :
- lorsqu'une machine A fait déjà confiance à B et que B fait déjà confiance à A, la demande de join peut être automatiquement acceptée.

Avantages :
- réutilise la logique existante ;
- peu intrusive ;
- faible coût de développement ;
- cohérente avec le modèle de sécurité actuel.

Limites :
- ne supprime pas à elle seule la notion de session à cibler ;
- nécessite un mécanisme complémentaire pour découvrir automatiquement la session ou le profil à rejoindre.

## 7.2. Solution B. Trusted devices persistants côté serveur

Principe :
- le serveur conserve une trace des autorisations de connexion entre devices déjà approuvés.

Avantages :
- simplifie la gestion des auto-connexions ;
- facilite la révocation centralisée ;
- permet d'exprimer des droits plus fins.

Limites :
- introduit davantage de logique de sécurité côté backend ;
- demande une bonne gestion de l'intégrité des autorisations ;
- ne doit pas remplacer la vérification cryptographique locale.

## 7.3. Solution C. Challenge asymétrique pur

Principe :
- chaque reconnexion est validée par signature d'un nonce.

Avantages :
- fort niveau de sécurité ;
- très bon alignement avec les clés RSA déjà présentes ;
- évite de faire confiance à un secret partagé statique.

Limites :
- nécessite de faire évoluer le protocole d'authentification de session ;
- demande une gestion propre des nonces, expirations et rejets.

## 7.4. Solution D. Certificats ou PKI

Principe :
- chaque machine reçoit un certificat signé par une autorité de confiance.

Avantages :
- modèle standard en cybersécurité ;
- forte traçabilité ;
- révocation plus structurée.

Limites :
- complexité élevée ;
- coût d'exploitation plus important ;
- probablement surdimensionné pour l'état actuel de ByteSync.

Conclusion technique :
- la solution C est la plus solide ;
- la meilleure trajectoire produit est B + C, en s'appuyant sur A comme fondation existante.

## 8. Risques de sécurité d'une approche sans mot de passe

Supprimer la saisie manuelle ne signifie pas supprimer les risques. Le modèle passwordless déplace la confiance vers la machine et ses secrets locaux.

Les risques principaux sont les suivants.

## 8.1. Compromission locale du poste

Si un attaquant obtient un accès complet au poste et vole la clé privée locale :
- il peut se faire passer pour la machine ;
- il peut profiter de son statut de trusted device ;
- il peut contourner l'absence de mot de passe utilisateur.

Ce risque est le plus important.

## 8.2. Protection locale imparfaite des clés

Dans ByteSync, les clés locales sont chiffrées dans les paramètres applicatifs.

Cette protection complique l'extraction, mais elle ne constitue pas un coffre matériel.

Le secret de chiffrement local dépend notamment :
- d'une partie embarquée dans le code ;
- d'une variable d'environnement ou d'un identifiant machine ;
- d'une valeur de secours en dernier ressort.

Conséquence :
- cela protège contre une récupération triviale ;
- mais un attaquant avancé ayant compromis la machine peut potentiellement reconstruire ce secret.

## 8.3. Usurpation après rotation de clé

Si une machine renouvelle sa paire de clés :
- l'ancienne confiance ne doit plus être considérée comme suffisante ;
- il faut imposer une révalidation ;
- sinon une incohérence d'identité peut apparaître.

## 8.4. Rejeu de jetons persistants

Si l'on se contente d'un token long terme stocké localement :
- le vol du token peut suffire à rétablir la connexion ;
- la protection est plus faible qu'un challenge signé.

## 8.5. Autorisations trop larges

Un trusted device ne doit pas devenir automatiquement "tout puissant".

Il faut limiter :
- les profils accessibles ;
- les sessions visées ;
- les plages d'utilisation ;
- les actions autorisées si besoin.

## 8.6. Révocation insuffisante

Si un poste est perdu, réinstallé ou compromis, il faut pouvoir :
- supprimer immédiatement sa confiance ;
- empêcher toute reconnexion automatique future ;
- journaliser les tentatives de reconnexion rejetées.

## 9. Recommandations d'architecture

## 9.1. Recommandation principale

Pour ByteSync, la meilleure approche est :

- conserver l'appairage manuel initial ;
- conserver la logique de trusted public keys ;
- ajouter un registre de trusted devices persistants ;
- ajouter un challenge-réponse signé pour chaque reconnexion automatique ;
- supprimer la saisie manuelle du `sessionId` et du `sessionPassword` uniquement après validation préalable du device.

## 9.2. Contre-mesures recommandées

Pour rendre le modèle robuste, il est recommandé d'ajouter :
- une date d'expiration sur les autorisations de trusted devices ;
- une révocation manuelle et immédiate ;
- un journal des auto-connexions ;
- une option de validation explicite lors d'un changement de clé publique ;
- une politique configurable : auto-accept désactivé, partiel ou total ;
- si possible à terme, une meilleure protection locale des clés via stockage sécurisé OS ou TPM.

## 9.3. Position de sécurité à défendre

La position la plus défendable est la suivante :

"ByteSync peut fonctionner sans mot de passe visible par l'utilisateur, à condition que la confiance ne repose pas sur l'absence de secret, mais sur une identité machine forte, persistante et déjà validée."

Autrement dit :
- le mot de passe n'est pas remplacé par rien ;
- il est remplacé par une preuve cryptographique de possession d'identité.

## 10. Conclusion

L'étude montre que ByteSync ne repose pas uniquement sur un identifiant de session et un mot de passe, mais sur une architecture de sécurité multicouche.

Le système dispose déjà d'éléments essentiels pour mettre en place une connexion automatisée entre machines de confiance :
- identité RSA persistante ;
- clients de confiance enregistrés localement ;
- contrôle de clés publiques ;
- authentification par signature ;
- échange sécurisé de la clé de session.

La mise en place d'une connexion sans intervention manuelle est donc faisable, mais uniquement dans un cadre contrôlé :
- machines déjà appairées ;
- clés publiques déjà approuvées ;
- challenge signé pour chaque reconnexion ;
- possibilité de révocation.

La recommandation finale est donc de faire évoluer ByteSync vers un modèle de `trusted devices passwordless`, fondé sur les clés publiques déjà présentes dans le projet, plutôt que vers un simple système de token persistant.

## 11. Proposition d'explication orale

Pour présenter le sujet simplement à l'oral, on peut le résumer ainsi :

"Aujourd'hui, ByteSync sécurise la connexion en plusieurs couches. Le client s'authentifie d'abord au serveur avec un JWT. Ensuite, entre machines, la vraie confiance repose sur des clés publiques déjà validées et sur des signatures numériques. Le mot de passe de session existe encore, mais il n'est pas envoyé en clair. Pour automatiser la connexion entre deux machines déjà connues, la meilleure solution n'est pas de supprimer la sécurité, mais de remplacer la saisie manuelle par une reconnaissance cryptographique automatique du device. En pratique, cela revient à dire : une première validation humaine, puis des reconnexions automatiques grâce à une preuve par signature et à une autorisation persistante révocable."

## 12. Ouvertures possibles pour la suite

Si cette étude doit déboucher sur une suite technique, les prochaines étapes les plus pertinentes sont :
- définir le modèle de données d'un `TrustedDeviceGrant` ;
- concevoir le protocole de challenge-réponse ;
- définir les règles de révocation et de rotation de clé ;
- préciser l'UX d'appairage initial et de gestion des devices de confiance ;
- évaluer si le stockage local des clés doit être renforcé par des mécanismes natifs du système d'exploitation.
