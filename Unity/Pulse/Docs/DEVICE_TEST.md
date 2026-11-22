# Android device smoke test — 14 November 2026

Device: Samsung Galaxy S21 (SM-G991B), Android 15, ARM64, 1080 × 2400 display. Connected and authorized through USB ADB using Unity 6000.6.9f1's bundled platform tools.

Installed the existing `Builds/Android/Pulse.apk` (30,816,860 bytes, version 1.0.0/code 2). No previous installation of this package was detected. Launch activity: `spetko.ondrej.TestPulse/com.unity3d.player.UnityPlayerGameActivity`.

- Installation and cold launch succeeded. Title, gameplay, pause, results and settings screens rendered on the phone.
- Play and menu taps responded. A horizontal swipe was injected during gameplay; steering accuracy, reversals and simultaneous touches still require a dedicated controls test.
- Gameplay advanced, scoring increased, and a collision ended the run at score 130.
- The on-screen pause button paused the run. Two separated captures retained score 112 and identical scene content.
- Resume continued the run. Home followed by bringing the app back to the foreground produced the pause screen, requiring deliberate resume.
- Injecting Android Back from the paused screen resumed gameplay, confirmed by the state log. An earlier Back injection immediately after starting the run was inconclusive; gameplay Back and settings Back should receive a dedicated repeat test.
- Force-stop followed by cold launch returned to the title screen with personal best 0130 preserved. This test score remains in the phone's local save.
- No process crash was observed during this smoke test. The app was left running on its title screen.

Startup diagnostics need investigation: Unity logged a `ClassNotFoundException` for `com.google.android.play.core.assetpacks.AssetPackManager` and messages about unavailable or stripped URP post-processing shaders, including Bloom. The app continued rendering and playing; this smoke test does not establish the cause or visual impact of those diagnostics.

Local evidence is in the ignored `Screenshots/Android/` directory: `title.png`, `paused.png`, `paused-later.png`, `background-return.png`, `back-key.png` (results after resuming), `settings.png`, `relaunch.png`, and `device-logcat.txt` from the first process. The first gameplay capture was saved as `paused.png` and later replaced by the verified pause capture.

Still pending: controlled steering and multitouch checks, shield/pulse interactions, motion sensors, audible sound and haptics, settings persistence after modification, lock/unlock and interruptions, sustained performance/thermal profiling, and additional devices/aspect ratios. Follow `ANDROID_QA.md` for the full release gate.

## Steering and trail revision

Removed the decorative green player outlines and title orbit. The blue shield/pulse indicator remains. Maximum steering is now 45 degrees from forward, reduced from 78. The wake samples actual field displacement at fixed travel distances; older marks retain the travelled path during reversals. Background motes use the same heading, and the trail remains visible while paused.

All 16 EditMode tests passed, including both steering extremes and wake frame-rate consistency, turning history, pause and restart. Deterministic editor review confirmed 45-degree full steering and a curved reversal; captures are in `Screenshots/Android/steering-right-editor.png` and `steering-reversal-editor.png`. Review logs confirmed those angles. The final compile and console check reported no current errors.

Built and installed the final revised APK (30,814,760 bytes) as an in-place update. The phone retained personal best 3986 and its existing motion-control preference. The final title capture confirms the remaining title ring is gone. A final run started and paused successfully, with the device log reporting `max steering angle=45`. Direction/reversal behavior was exercised deterministically in the editor; phone motion steering still needs hands-on evaluation. Evidence: `title-no-orbits.png`, `steering-final-paused.png`, and `steering-final-logcat.txt`. The app was returned to the title screen.

The build succeeded with zero errors and four warnings, recorded in `AndroidBuildReport.json`: three TextMeshPro IL2CPP compilation advisories and a pending editor-code compilation warning. Editor compilation subsequently completed successfully. Earlier startup asset-pack/shader diagnostics remain outside this revision's scope.

The Unity editor was left out of Play Mode with background execution and auto-tick overrides off. `AgentScripts/VerifySteering.cs` is an editor-only review helper. Pre-edit source copies are in ignored `Screenshots/Android/restore-steering/`; new wake source/tests and this helper have no pre-edit counterpart. Guidance used: Unity CLI `integration-advanced.md`, `build-run-test.md`, `playmode-verification-loop.md`; Pipeline script/build/capture/lifecycle/runtime command documentation; and the uGUI skill. Unity 6.6 Vector2 and Mathf API pages were checked.

## Motion-control revision

The requested "60% angle" was interpreted as a 60-degree steering limit, replacing the prior 45-degree limit. The phone was using Tilt with sensitivity at 2. Its settings were inspected without changing them.

Tilt now reads gravity direction as an angle, preferring the gravity sensor to raw acceleration when available. Full input requires 30 degrees of phone tilt at sensitivity 1 (15 degrees at sensitivity 2). At either input limit, additional tilt shifts the reference instead of creating a flat region that has to be traversed on the way back. This means the neutral position can shift after excessive tilt; starting a run or resetting the gesture on pause/settings recalibrates it. A small center deadzone and progressive response suppress tiny movements. A single exponential filter has a 42ms outward time constant and a faster 28ms release/reversal time constant. The simulation consumes this already-filtered command directly.

Twist stores a separately bounded rotation command. Sensitivity scales the incoming rotation rather than multiplying the previous output every frame, preventing stationary input from drifting due to feedback.

All 29 EditMode tests passed. `AgentScripts/VerifyMotion.cs` also injected gravity samples through the actual Input System -> PulseInput -> PulseSimulation path in Play Mode at sensitivity 2. Results: full steering 60 degrees; first return sample after a 70-to-69-degree phone-angle change 57.65529 degrees; reversal after three 60Hz samples -36.24581 degrees. Current console and compile errors were zero. These are deterministic simulated-input checks; physical comfort and sensor timing still require hands-on evaluation.

Pre-edit source copies are in ignored `Screenshots/Android/restore-motion/`. Guidance for sensor APIs was checked against the installed Input System 1.20 `Sensor.cs`, `devices-sensors.md`, `supported-sensors-reference.md`, `sensors-introduction.md`, and `query-sensors.md`.

The initial motion APK installed successfully using `adb install -r`, but foreground checks initially waited for the user to unlock the phone. After unlocking, title/gameplay/pause rendered correctly and Android confirmed an active gravity sensor connection for the app. Personal best 3986 remained intact. `motion-final-paused.png` and `motion-ready.png` captured the earlier lock screen, not gameplay; use `motion-unlocked.png` and `motion-unlocked-game.png` for that first foreground check.

During this follow-up, the sensor registration record's 200ms period was initially mistaken for the actual event rate. The detailed gravity event timestamps showed approximately 20ms intervals before explicit frequency configuration. The input now requests 60Hz for each enabled steering sensor. The final installed APK (30,819,408 bytes) built with zero errors and three TextMeshPro IL2CPP advisories. On this phone, its gravity event history showed approximately 10ms intervals, with the app's gravity sensor connection active. The hardware service's event history is not a measurement of application callback latency. Evidence: `motion-60hz.png`, `motion-final-logcat.txt` (60-degree run start and pause), and `motion-sensorservice-final.txt`.

The final phone run started and paused successfully with no observed process crash. The app was returned to its title screen. The temporary USB keep-awake setting was restored to its original value 0, and Unity auto-tick was turned off. Hands-on steering comfort and reversals still require the user's evaluation; the deterministic 29-test and sensor-injection checks above establish the control logic.
