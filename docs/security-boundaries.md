# Security boundary changes

Date: 2026-11-13. Local implementation on `fix/security-boundaries`, based on `397a1d1d781ae9eaa979d3cfe48b9ac7256f99d4`. A read-only check of `origin/master` returned the same commit before changes. The checkout was initially clean. The actual solution targets .NET 10; validation used SDK 10.0.204 on Windows.

This is an implementation and local validation report, not evidence of a deployed fix or of a compromise. No hosted service was tested. Security-word design, AEAD migration and update signing remain separate work.

## Implemented relay boundary

### Possession, identity and authentication epoch

`AuthenticationProtocol` derives the identity from SHA-256 of canonical DER SubjectPublicKeyInfo after fully importing the existing PKCS#1 RSA public key. The full uppercase 64-character fingerprint is the security identity. Keys must be 2048–8192 bits; trailing key data is rejected. Instance identifiers consist of that fingerprint, an underscore and a canonical GUID. Multiple instances can prove possession of the same durable key independently.

The client retains its existing RSA private key. Login first obtains a random 256-bit server nonce with a two-minute lifetime, then signs a length-prefixed canonical binary message with RSA-PSS/SHA-256. The message binds the domain, authentication epoch, relay audience, protocol version, challenge ID, nonce, expiry, fingerprint, instance, software version and platform. The relay stores the exact binding, consumes the Redis challenge using atomic `GETDEL`, then verifies the signature. Failed verification does not issue tokens or adopt an existing instance. Redis 6.2 or newer, or a compatible service implementing GETDEL, is a deployment prerequisite.

The current authentication epoch is 1 and protocol is V3. Software version remains a separate compatibility check and cannot substitute for proof of possession. The legacy short `ApplicationSettings.ClientId` remains a display/trust alias; it cannot claim a server identity or an old instance. There is no first-key-wins migration of old aliases. Existing mismatched or legacy instance records are rejected.

JWTs carry fingerprint and epoch claims. Both JWT validation paths check signature, HS256, issuer, audience, lifetime and the live record/claim association. Refresh records also carry the epoch; an old refresh token cannot mint a verified JWT. No weak login fallback was added. Public challenge/login/refresh endpoints are POST; announcements remain public. The local key and trusted-key lists are not regenerated or silently cleared.

### Session authorization and joining

The registered MediatR `SessionAuthorizationBehavior` protects session, inventory, member, synchronization, trust and transfer requests before their handlers run. It checks verified identity, session existence/removal, session epoch/protocol, current membership or an eligible expiring prejoin attempt, allowed state, and operation-specific resource ownership. A reflection test enumerates all session request types and detects missing authorization markers. Profile/lobby operations have their own ownership checks because their authorization context differs from a session.

Members can still read other members' inventories/sources within their session. Source/node mutations must address the caller's resources, including collision checks inside the repository update. Action acknowledgements must reference the real source/target node. Route/body identifiers are compared for session, member, profile and lobby endpoints. All referenced actions are preflighted before batch handlers run.

`beginJoin` creates a controlled five-minute prejoin attempt before the legitimate trust exchange and returns only instance IDs. Detailed membership remains restricted to current members. The attempt binds the verified requester, public key and chosen current validator. Repeated password-exchange setup preserves an eligible existing attempt and its trust checks. The validator must equal the authenticated caller. Finalization requires the authenticated joiner, matching validator, session, identity, nonempty unique proof and expiry. Finalization consumes the proof while holding the session Redis lock across transaction commit; notifications follow successful commit. The old short-ID prefix shortcut was removed. Same-key shortcuts require the full verified fingerprint and instance namespace.

Profile slots identify their original instance in addition to the durable fingerprint, preserving distinct slots for multiple instances of one identity. New profiles and lobbies carry the current epoch. Legacy remote relationships cannot silently become verified relationships. Local profile files are preserved.

### SignalR

Negotiation binds the SignalR user to `epoch:instance`; connection callbacks refuse legacy users and unverified records. Client/session/lobby group names include the authentication epoch. Reconnection rechecks saved session/lobby subscriptions against current membership and epoch, rather than trusting persisted group names. Joining/quitting applies group changes to all known distinct connections of the instance. Token-bearing negotiation responses and session credential payloads are no longer logged.

There are no client-invocable synchronization mutations over SignalR in this serverless design; mutations enter authenticated HTTP/MediatR handlers. SignalR connection callbacks still depend on the deployment's trusted Azure upstream configuration. A live Azure test has not been performed.

### Storage object authority

A transfer must match its authorized route/session, verified upload owner, type, action ownership and allowed part range. The relay creates or loads an immutable file record, chooses a random storage object ID, snapshots recipients and provider, and replaces the request's free-form object selection with the stored definition. Synchronization downloads require the recipient to be a target of every action in the transferred archive, as well as a current session member. Other transfer types snapshot the other current members. Downloads require an uploaded registered part. Totals cannot contradict recorded parts.

Azure/R2 object keys are formed only from the server-generated ID and validated part number. URLs are scoped to one part and one operation: Azure blob write/read SAS or R2 PUT/GET, with five-minute expiry. The record's provider is used even if the default provider configuration later changes. Part bytes may be deleted after acknowledgement, but the authorization record is retained until session cleanup or its existing two-day cache expiry so the final acknowledgement remains authorized. A caller cannot substitute another session's object by changing body path fields.

Signed URLs remain bearer capabilities until expiry. Membership revocation prevents new URLs; it does not revoke a previously issued URL. Upload owners can overwrite their own part while a write URL is valid. Provider-level immutability, signed content hashes and AEAD are separate decisions.

## Implemented client filesystem boundary

### Local permissions and replicated representations

`LocalSourceAuthorizationService` owns local permissions independently of replicated sources, inventories and profile contents. Local application settings persist exact `(key fingerprint, canonical root, file/directory type)` permissions. A runtime grant additionally binds the current session object, session ID, source ID, real node, local instance and root/type snapshot. A reused short session ID cannot inherit a previous session object's grants.

Only explicit local source selection creates a new permission. Network sources are checked against the authenticated DTO owner and actual node owner. A network echo of a local source must match an existing grant; it cannot change its root or type. Initial source/node loading after join has the same checks. Profile restoration can attach a new runtime source ID only to an already persisted exact local permission. Therefore unattended profiles remain possible after local enrollment, without prompting on every sync. Old profiles need their sources selected locally once; paths from profile contents do not grant themselves authority. Explicit local removal revokes the permission.

Inventory parts and action parts now carry `SourceId`; action parts also carry a strict source-relative path. Remote inventory ownership, source identifiers and logical relative paths are validated, and parent inventory references are rebuilt from the authoritative top-level inventory. This prevents a malicious inventory from acquiring local authority when an honest creator computes and forwards the plan.

### Plan validation and execution

The complete received/generated plan is checked before installation, organization or execution. Structural validation includes unique safe action IDs, valid operators/types, required timestamps/synchronization mode, complete part bindings and valid relative paths. Local parts are resolved from the local grant, never from a peer's root. Resolver callbacks are excluded from JSON and installed only after every action passes. A file grant accepts only the empty relative path, so it cannot authorize siblings. Directory checks use a separator boundary and the platform's casing rules.

Execution resolves again and rechecks permission. The boundary covers content reads/fingerprints/uploads, full copy staging and replacement, delta basis/output/replacement, deletes, directory creation, file timestamps, downloaded archives and finalization. Upload slicing and download merging/finalization repeat access checks. Multi-file archives are completely checked for unexpected IDs, duplicate entries and path forms before extraction.

Internal cache/profile/session components are validated separately; a peer machine name is no longer used to select a cache path. Temporary replacements use random sibling names and exclusive creation. An invalid complete plan performs no synchronization mutation. This is validation atomicity, not a transactional filesystem: a valid batch that later loses permission or encounters I/O failure may have completed earlier authorized actions.

### Native access and deliberate restrictions

`LocalPathSafety` combines strict path syntax, reparse/link rejection and native operations. It does not treat two textual checks as a filesystem race solution.

* Windows traverses directories with no-delete/no-write sharing leases, opens leaves with OPEN_REPARSE_POINT, checks the opened handle and rejects hard-linked files. Replacement pins the source and parent handles and uses handle-relative NT rename within the same parent. Parent directory pinning and a nonempty parent protect that replacement against directory substitution. Real full-copy/delta, junction, hard-link and directory-substitution tests ran on this Windows host.
* Linux/macOS use component-wise openat with O_NOFOLLOW and directory descriptors, fstat of the opened leaf, mkdirat/unlinkat, and relative native rename with exclusive no-overwrite flags when requested. File streams use the opened descriptor. Linux x64/arm64 and macOS x64/arm64 ABI layouts were inspected against platform headers; they have not been executed here. Unsupported Linux architectures fail closed.
* Symlinks, junctions/reparse points and hard-linked file content are refused, including links beneath an authorized directory. UNC/device sources are refused. Backslash, colon, absolute paths, empty/dot/dot-dot components and controls are refused in the portable relative representation. Windows additionally rejects device names, invalid characters and trailing dots/spaces. This excludes some valid Unix filenames and network-drive/link workflows; there is no automatic fallback.
* Directory deletion is nonrecursive. Empty directories can be removed, but a nonempty tree must be represented by individual authorized child operations before its directory is removed. Timestamp operations apply to regular files; unsupported special files fail closed.

Microsoft documents that a directory must be empty to establish a reparse point: [Reparse points](https://learn.microsoft.com/en-us/windows/win32/fileio/reparse-points). Native declarations were checked against [Apple stat.h](https://github.com/apple-oss-distributions/xnu/blob/main/bsd/sys/stat.h), [Apple stdio.h](https://github.com/apple-oss-distributions/xnu/blob/main/bsd/sys/stdio.h) and [glibc Linux stat layout](https://github.com/bminor/glibc/blob/master/sysdeps/unix/sysv/linux/x86/bits/struct_stat.h). These checks are static evidence, not cross-platform runtime validation.

## Architecture choices and rejected alternatives

| Choice | Alternative rejected | Reason |
| --- | --- | --- |
| Durable RSA possession and full canonical fingerprint | Trust a supplied instance/short alias, or JWT validation alone | Validation of a token cannot repair an unproved initial identity. |
| Atomic expiring Redis challenge | Process-local nonce or read-then-delete | Multiple workers must agree on single consumption. |
| Explicit epoch/protocol break | Keep the old login beside the new one | A weak reachable login would retain the vulnerability. |
| Recreate legacy remote relationships | Let the first key claim a legacy alias/profile | There is no authenticated legacy binding to migrate automatically. |
| Local grants plus SourceId resolution | Import permissions from a profile/source echo or sanitize peer roots | Replicated data cannot be the authority for local access. |
| Persist locally enrolled permissions | Ask for confirmation at every sync | Local permissions preserve unattended operation without substituting prompts for enforcement. |
| Native no-follow access and initial link refusal | GetFullPath/StartsWith or repeated path checks alone | Text does not pin the filesystem object accessed. Supporting links safely needs additional design. |

## Validation

The final non-incremental build and each test command were run separately. Final commands and result counts are recorded below. TRX and coverage artifacts are retained locally under `artifacts/security-boundaries/`; build/test logs are local ignored files. They contain no intentionally logged tokens, private keys or session secrets. Test keys and sentinels are synthetic.

| Final suite | Passed | Failed | Not executed (TRX, including OS exclusions) |
| --- | ---: | ---: | ---: |
| server | 364 | 0 | 0 |
| client | 1232 | 0 | 12 |
| functions | 66 | 0 | 0 |
| common | 53 | 0 | 0 |
| local-integration | 40 | 0 | 6 |

**1755 passed, zero failures; 186 security cases are included in these totals. 18 cases did not execute.** NUnit console totals exclude some OS-filtered cases; the table counts every NotExecuted TRX result. Build: zero errors, 670 warnings (including existing nullable, obsolete API and dependency vulnerability warnings). `git diff --check` passed.

Reproduction (build and tests are separate commands):

```powershell
rtk proxy dotnet build ByteSync.sln --no-incremental --no-restore --verbosity quiet
rtk proxy dotnet test tests/ByteSync.ServerCommon.Tests/ByteSync.ServerCommon.Tests.csproj --no-build --no-restore --filter 'FullyQualifiedName!~.Repositories.' --collect 'XPlat Code Coverage'
rtk proxy dotnet test tests/ByteSync.Client.UnitTests/ByteSync.Client.UnitTests.csproj --no-build --no-restore --collect 'XPlat Code Coverage'
rtk proxy dotnet test tests/ByteSync.Functions.UnitTests/ByteSync.Functions.UnitTests.csproj --no-build --no-restore
rtk proxy dotnet test tests/ByteSync.Common.Tests/ByteSync.Common.Tests.csproj --no-build --no-restore
rtk proxy dotnet test tests/ByteSync.Client.IntegrationTests/ByteSync.Client.IntegrationTests.csproj --no-build --no-restore --filter 'FullyQualifiedName~TestSynchronizationActionHandler|FullyQualifiedName~EnvironmentService_Tests|FullyQualifiedName~FullInventoryRunner_IntegrationTests|FullyQualifiedName~InventoryBuilderAccessHandling_IntegrationTests'
```

Do not run a coverage collector and another test process against the same output assembly concurrently. Final collection used one process per test output. Superseded trial output is under the separate `history-*` directory; only `final/validation.json` and its referenced reports are authoritative.

Line coverage of selected boundary source files, merging duplicate/state-machine sequence points within each report (not whole-project or Sonar new-code coverage):

| Source | Covered / executable lines | Coverage |
| --- | ---: | ---: |
| `AuthenticationProtocol.cs` | 28 / 29 | 96.6% |
| `AuthenticationChallengeService.cs` | 41 / 41 | 100.0% |
| `CloudSessionProfileService.cs` | 79 / 84 | 94.0% |
| `TransferAuthorizationService.cs` | 51 / 59 | 86.4% |
| `ClientAuthentication.cs` | 8 / 8 | 100.0% |
| `SessionAuthorizationBehavior.cs` | 114 / 127 | 89.8% |
| `LocalPathSafety.cs` | 153 / 243 | 63.0% |
| `LocalSourceAuthorizationService.cs` | 104 / 110 | 94.5% |

The 85% target is **not met for LocalPathSafety** on this Windows host, in particular because native POSIX branches did not execute. No exclusions were added to hide those branches. The PR-wide 80% Sonar gate has **not been demonstrated**. Cross-platform runtime tests and the actual new-code analysis remain release gates.

Machine-readable evidence: [final validation summary](../artifacts/security-boundaries/final/validation.json).

Security tests cover real RSA proofs, expiry/replay, concurrent consumption (30 attempts, one winner using an atomic Redis fake), instance binding, old token/refresh rejection, real registered JWT bearer authentication, creation, join proof binding/replay, profile ownership/multiple-instance slots, current SignalR subscriptions, every session request's outsider rejection, transfer ownership/parts/lifecycle and offline Azure SAS scope. Client cases exercise exact file grants, hostile paths and source/node impersonation, forged inventory propagation, XML permission persistence, revocation, complete-plan rejection, links/junctions/hardlinks, real copy/delta and locally enrolled automated restore. Existing local integration tests now explicitly grant their fixture sources through the real registry.

### Validation not performed

* No tests against production, staging or hosted ByteSync, Redis, Azure SignalR, Blob Storage or R2. Server repository and Functions integration suites require configured services and were excluded. R2 upload/download and resume integration classes were excluded explicitly.
* Atomic Redis behavior is implemented with GETDEL and tested with a fake; a real supported Redis deployment, RedLock/transaction failure and failover behavior must be exercised in an isolated environment.
* Docker is installed but its engine was unavailable; no ready Linux/macOS runner was available. POSIX native syscall/ABI behavior, filesystem-specific edge cases and packaged applications need runtime tests on each supported OS/architecture.
* Windows ACL-denial integration cases self-skipped because this host did not enforce their synthetic permission changes. POSIX-only tests cannot run on Windows.
* No Sonar PR analysis, production-scale/load/rate-limit evaluation, complete UI/manual trust exchange, or live multi-machine automatic synchronization was run. Local tests do not establish a production security certification.

## Future rollout proposal (not executed)

1. Review this patch and approve the breaking protocol, restrictions and enrollment experience. Complete native multi-platform CI, isolated Redis/SignalR/storage end-to-end tests, load/abuse checks and the coverage gate. Confirm the Redis version before rollout. Back up local RSA/settings/trust/profile data through the existing supported process; do not collect private keys centrally.
2. Prepare matched V3 clients and relay together in an isolated environment. Verify manual join, multiple instances, reconnection, profile automation, full/delta transfers and expiration/revocation. Distribute the compatible clients before the cutover window if desired, accepting that they will refuse the old relay protocol until cutover. A separate explicitly selected legacy client can remain outside the new service only under a separately approved risk decision; coexistence does not close the old login flaw.
3. During an announced maintenance window, stop/drain the old relay writers and synchronization jobs. Disable the old login path on every reachable relay instance. Do not run mixed old/new relay workers against a shared authorization namespace. Invalidate old refresh/JWT assurance through the epoch checks; rotate signing secrets as an additional operational measure if approved. Terminate old SignalR connections or provision a fresh hub and switch upstream endpoints; epoch groups isolate new delivery but do not sanitize an old reachable relay. Wait for previously issued legacy SAS/R2 URLs to expire or revoke them operationally before declaring cutover complete.
4. Deploy the matched relay/client V3 pair. Create fresh sessions; epoch-0 sessions cannot continue. Reauthenticate with the existing private key, recreate cloud profile/lobby relationships, and perform the normal peer trust exchange using retained local trusted keys. Do not map a legacy alias to whichever key appears first. Old profile files remain available for manual reconstruction; migration of remote relationships would require a separately designed proof-based process.
5. Enroll each local source through local selection once. Subsequent profile restore must match the stored permission exactly and can run unattended. Explain rejected link/UNC/special-name workflows before enabling automated jobs. Verify fresh full and delta syncs, reconnection and all instances before enabling scheduled work.
6. Observe proof/epoch/authorization failures using identifiers and status codes only. Monitor retry behavior, Redis consumption, transfer metadata retention and grant failures. Never add request-body, JWT, private-key, credential or signed-URL logging for diagnostics.

Old clients fail the challenge/epoch/protocol boundary; claiming a newer software version does not help. Existing sessions, transfers, remote profiles/lobbies and SignalR subscriptions are not silently promoted. The constants are code-level epoch controls; a later epoch requires a coordinated code release and the same operational invalidation steps.

### Safe rollback

Keep the strong authentication/authorization boundary active. Roll back to a previously tested V3 build or disable the affected operation/service while correcting it. Preserve keys, trust lists, settings, local grants and profile files; do not downgrade their meaning or auto-import profile paths. Quarantine or expire failed-epoch server state and issue fresh tokens/sessions after recovery. Do not restore an old vulnerable login, lower the minimum protocol/epoch, remove membership checks, or fall back to raw peer paths. If no compatible build exists, fail closed and keep synchronization unavailable until repaired. A deployment/database rollback to the old relay is not a safe security rollback.

## Residual risks and decisions for Paul

Approval is still needed for the coordinated downtime and client/relay release; recreation of existing sessions and remote profiles; the initial local source enrollment UX and user communication; rejecting links, UNC sources, hard links, special names and recursive empty-tree deletion; five-minute URL expiry and retained transfer metadata; and the supported native OS/filesystem/architecture matrix. No such rollout decision was executed here.

The local settings and durable private key remain trusted local state. Malware running as the user, a privileged local filesystem/mount adversary, or a compromised client can alter that state. Native handle/descriptor access prevents following substituted links, but does not promise rollback of already opened I/O or protection against every concurrent directory relocation/mount change. On Unix a directory pinned by descriptor can itself be renamed by another process; operations then refer to the pinned object. Hard links created after the opened-file check, and arbitrary privileged local renames, require a stronger OS isolation policy if they are included in the threat model. Inventory metadata enumeration still uses existing scanner APIs; content reads are constrained, but concurrent Unix metadata enumeration has not been proven race-free. Local revocation blocks subsequent resolutions and transfer slices, not bytes already read or written.

Relay authorization checks use current repository state before dispatch; they do not make every handler, notification and concurrent membership change one serializable transaction. Final join-proof consumption is locked/transactional. Already issued URLs and in-flight requests retain their bounded capabilities. Distributed state, failover and departure/reconnect races require isolated integration validation.

HTTP bodies/archives still need a broader resource-abuse budget/rate-limit review. Peer possession proves control of a key, not that the key owner is honest. A session member may still request destructive actions within sources the local user authorized. The relay cannot inspect encrypted file contents. Cryptographic content integrity/AEAD, security-word entropy and update authenticity remain separate tracked work. Existing dependency vulnerability warnings were observed during build; this patch does not update those packages.

No push, public PR, publication, deployment, production configuration change or external message was performed. Network use was limited to read-only repository-head/documentation checks; all executed tests used local files, synthetic credentials, fakes or offline signing.
