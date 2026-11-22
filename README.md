# Breakthrough — Processing to Unity

The original Processing/Android sketch is preserved at the repository root. The Unity 6.6.9f1 project is in **Unity/Pulse**. Open that directory in Unity Hub, then open `Assets/Pulse/Scenes/Pulse.unity` and press Play.

## Playing

You are the mint signal at the center of a scrolling field. Collect gold for points and blue shields to survive collisions. Each obstacle you break through also earns points. Gold begins at 100 points and breakthroughs at 40. Each additional 40 seconds adds 100 points to gold and 40 to breakthroughs, with the increase applied continuously during the run. Bank three shields to unlock ten seconds of Pulse protection. While Pulse is active, each shield pickup restores one-third of the timer, capped at ten seconds. Speed builds, releases, then builds toward a higher ceiling.

- Phone: drag horizontally below the score bar to steer and lift to go straight. When all three shield segments light up, double-tap the playfield to activate Pulse. The first run for each steering mode starts with a short primer. Settings offers Tilt and Twist with a Recenter action, sensitivity, audio levels, vibration and reduced motion. If a sensor is unavailable, Settings shows the steering mode in use.
- Editor: drag with the mouse or use A/D or arrow keys. Space starts/retries or activates a charged pulse. Escape pauses/resumes or closes settings.
- Steering is limited to 60 degrees either side of forward. Motion input has a gentle center response and discards excess tilt at the limit, so turning back responds immediately. The trailing wake follows the travelled path. Blue segments indicate banked shields, and a draining inner arc shows the active Pulse timer.
- Backgrounding the Android app pauses the run. Resume deliberately after returning.
- Pulse saves the personal best, completed-run count, and settings on the device. Game over shows the final number and either highlights a new best or shows the saved best. The original Processing SQLite database is not migrated into Unity's save data.

## Build Android

The Unity Editor must have Android Build Support, SDK/NDK and OpenJDK installed. Choose **Pulse > Configure Android**, then **Pulse > Build Android APK**. Output: `Unity/Pulse/Builds/Android/Pulse.apk`. The APK uses Unity's debug signing identity for local testing.

The Android build uses portrait orientation, ARM64 IL2CPP, OpenGL ES 3, a minimum of Android 8 (API 26), and a target of Android 16 (API 36). The package ID is `spetko.ondrej.TestPulse`, with version 1.0.0 and version code 2. Upgrades require the existing package ID and signing key. The app appears as Breakthrough with the mint comet launcher icon.

For a release-signed `.aab`, set the four environment variables below in the process launching Unity, then choose **Pulse > Build signed Android App Bundle**:

```
PULSE_KEYSTORE_PATH
PULSE_KEYSTORE_PASSWORD
PULSE_KEY_ALIAS
PULSE_KEY_ALIAS_PASSWORD
```

Never commit a keystore or password. Release signing and store submission are separate from a locally testable APK. Before a public release, complete the physical-device checklist in `Unity/Pulse/Docs/ANDROID_QA.md`, confirm asset/music distribution rights, review the final merged manifest, and fill in the Play Console declarations for the actual shipping build.

## Implementation

`PulseSimulation` holds deterministic rules and swept circle collision checks, independently of scene rendering. The game recycles actors, effects and floating labels. Music streams and short sounds preload. uGUI and TextMeshPro render responsive menus inside the device safe area. Six procedural sprites provide the geometric gameplay art, and the Home screen uses the Fault Line wordmark.

The port corrects frame-dependent timing and incomplete run resets in the old sketch. Touch steering is on by default, with a maximum of three banked shields, a brief grace period after shield damage and automatic pause on interruption. Bluetooth versus is deferred by agreement.

Home and Game Over link to the all-time leaderboard. Firebase Authentication gives each installation a guest account, Cloud Firestore stores its highest completed-run score, and Google Analytics for Firebase measures gameplay time and key interactions on Android. See [Firebase setup](Unity/Pulse/Docs/FIREBASE.md) for SDK installation, project configuration, and event names.

Run the **Pulse.Tests** EditMode assembly in Unity Test Runner for timing, scoring, shield damage, pulse expiry, pause/resume, restart, seeding and speed-cycle coverage. The scene-generation helper `Unity/Pulse/AgentScripts/CreatePulse.cs` is versioned for reproducibility and can be run through the Unity Pipeline CLI; it rebuilds the dedicated Pulse scene and generated art, so do not use it to replace manual scene edits unintentionally.

The completed build, rule tests, visual review and remaining device/release gates are recorded in `Unity/Pulse/Docs/VERIFICATION.md`.
