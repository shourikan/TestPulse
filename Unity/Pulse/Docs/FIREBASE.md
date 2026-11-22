# Firebase setup

The [Breakthrough Firebase project](https://console.firebase.google.com/project/breakthrough-pulse/overview) has Android app `spetko.ondrej.TestPulse`, anonymous sign-in, and a default Cloud Firestore Standard database in Belgium (`europe-west1`). The Firebase SDK is optional for local gameplay. Install and configure it to enable online features.

1. From the Unity project directory, run `./InstallFirebaseSdk.ps1` in PowerShell before opening Unity. It installs the pinned official SDK and External Dependency Manager as local Unity packages. Unity may record those packages in `Packages/packages-lock.json` locally.
2. From `Unity/Pulse`, fetch the Android configuration using the Firebase CLI.

   ```powershell
   npx -y firebase-tools@latest apps:sdkconfig ANDROID 1:349995605139:android:3a59c51371006ac6ed4166 --project breakthrough-pulse --out Assets/google-services.json
   ```

3. Open Unity on the Android build target. Let External Dependency Manager finish resolving the Android libraries, or choose **Assets > External Dependency Manager > Android Resolver > Force Resolve**. Build the APK using **Pulse > Build Android APK**.

Firebase generates `Assets/StreamingAssets/google-services-desktop.json` from the Android config for leaderboard testing in the Unity Editor. The Android build sends Analytics events. Keep local Firebase configs and SDK packages out of source control.

Run `npx -y firebase-tools@latest deploy --only firestore:rules --project breakthrough-pulse` from `Unity/Pulse` to publish future changes to the rules. The leaderboard uses the automatic descending index on `score`.

## Leaderboard behavior

Cloud Firestore ranks the top 10 guest scores. A completed run can increase the installation’s online best, which the app saves locally and retries after a connectivity failure. Existing personal bests stay local. Guest identity is tied to the installation, so clearing app data can create a new guest. The rules restrict each guest to raising their own score. The game client reports scores, so the board is suited to casual play.

## Analytics events

| Event | Parameters and behavior |
| --- | --- |
| `play_time` | Sum `play_time_ms` for active gameplay. The game reports every 30 active seconds and flushes shorter intervals on pause, completion, or exit. Menus and paused time do not count. |
| `run_started` | `steering_mode` holds the effective steering mode. |
| `run_completed` | `score`, `duration_ms` and `new_record` describe the completed run. |
| `run_paused`, `run_resumed`, `run_abandoned` | Run lifecycle events. |
| `pulse_activated`, `settings_opened`, `leaderboard_opened` | Successful Pulse activation and menu opens. |

For totals in Analytics reports and explorations, use an event-scoped custom metric named **Active play time** with event parameter `play_time_ms` and unit **Milliseconds**. [Google Analytics custom metrics](https://support.google.com/analytics/answer/14239619)

To verify these events on a configured Android device, run `adb shell setprop debug.firebase.analytics.app spetko.ondrej.TestPulse`, open the app, and check **Analytics > DebugView** in Firebase. Start and end a run, pause and resume, and open Settings and Leaderboard. To stop debug reporting, run `adb shell setprop debug.firebase.analytics.app .none.`. [Firebase Analytics DebugView](https://firebase.google.com/docs/analytics/debugview)
