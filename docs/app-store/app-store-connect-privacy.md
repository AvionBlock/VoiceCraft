# App Store Connect Privacy Notes

Use this file as the source of truth when filling the VoiceCraft iOS privacy section in App Store Connect.

References:

- Apple App Privacy Details: https://developer.apple.com/app-store/app-privacy-details/
- Apple Privacy Manifest Files: https://developer.apple.com/documentation/bundleresources/privacy-manifest-files
- Apple Required Reason APIs: https://developer.apple.com/documentation/bundleresources/describing-use-of-required-reason-api
- Apple Local Network Privacy: https://developer.apple.com/documentation/technotes/tn3179-understanding-local-network-privacy

## Data Collection

VoiceCraft does not track users for advertising and does not share app data with data brokers.

Declare the following data types as collected only when anonymous telemetry remains enabled in the app. The first launch consent modal explains this collection and users can disable it later in Settings.

| App Store data type | Linked to user | Tracking | Purposes | Source in app |
| --- | --- | --- | --- | --- |
| Device ID | No | No | Analytics, App Functionality | Random telemetry token in `SettingsService.TelemetryToken` |
| Product Interaction | No | No | Analytics | Startup telemetry and feature diagnostics |
| Crash Data | No | No | Analytics, App Functionality | Manual crash dump upload from Crash Logs |
| Performance Data | No | No | Analytics, App Functionality | Runtime, CPU count, memory, app version, OS details |
| Other Diagnostic Data | No | No | Analytics, App Functionality | Platform diagnostics included with telemetry payloads |

Do not declare the following as collected by VoiceCraft unless the implementation changes:

- Audio Data: microphone audio is used for realtime voice chat with the selected VoiceCraft server. VoiceCraft does not retain voice recordings or send them to the telemetry endpoint.
- Contacts, Location, Photos, Videos, Payment Info, Health/Fitness, Browsing History, Search History, Advertising Data: not collected.
- User ID: VoiceCraft stores random app/server GUIDs for protocol functionality, but the telemetry token is not tied to an account or real-world identity.

## Permissions And Purpose Strings

`Info.plist` includes:

- `NSMicrophoneUsageDescription`: required for voice chat capture.
- `NSLocalNetworkUsageDescription`: required because users can connect to VoiceCraft servers on their local network.
- `UIBackgroundModes` with `audio`: required so voice chat audio can continue while the app is backgrounded.

## Privacy Manifest

`VoiceCraft.Client/VoiceCraft.Client.iOS/PrivacyInfo.xcprivacy` declares:

- No tracking.
- No tracking domains.
- The telemetry-related data types above.
- Required reason API categories used by app code or dependencies:
  - File Timestamp: `C617.1`
  - System Boot Time: `35F9.1`
  - Disk Space: `E174.1`
  - User Defaults: `CA92.1`

Review this manifest whenever adding iOS SDKs, analytics, ads, storage libraries, or new Apple APIs.

## Export Compliance

VoiceCraft uses standard operating-system networking and HTTPS/TLS for telemetry. If App Store Connect asks about encryption, answer according to the current build and Apple policy. For the current codebase, there is no custom cryptography implementation in the iOS client.

