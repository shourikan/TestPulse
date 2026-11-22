# Physical-device release gate

Editor checks and an APK build cannot establish device readiness. Record device/model, Android version, refresh rate and result for each pass.

- Install the APK on an ARM64 Android 8+ device; cold start and relaunch after force-stop. Confirm score/settings persistence. Existing installs with a different signing key require a separate test device/profile or uninstall with explicit awareness of data loss.
- Drag, release and reverse direction; test a second finger on the pulse button while steering. Try tilt and twist on a real sensor-equipped phone, calibrating at a comfortable neutral angle. Test sensor-free fallback.
- Collect gold, absorb damage with each shield count, activate a pulse, let it expire, die, retry repeatedly, and confirm each run begins without shields or residual effects.
- Pause during a pulse. Pulse time and scoring must remain frozen. Resume without a sudden steering jump.
- Background the app, lock the phone, receive an interruption, and return. The run must remain paused until Resume. Verify audio does not continue over other apps.
- Check 16:9, 19.5:9 and 20:9 portrait screens, a display cutout, gesture navigation and a portrait tablet. All controls must remain readable and tappable inside the safe area.
- Change music/SFX to zero and back; toggle haptics and reduced motion; restart the app and check settings persist. Listen to long music loops and rapid pickup overlap.
- Profile a 15-minute session on a lower-tier phone: CPU/GPU frame times, managed allocations, memory trend, thermals and battery. Target stable 60 fps without accumulating actors/particles. Verify 60/90/120 Hz screens produce the same game speed.
- Exercise Android Back from gameplay, pause, settings and results. Check resume after low-memory process recreation (returns to title; an in-progress run is not saved).
- Inspect the final release merged manifest and permissions. Bluetooth and multiplayer are deferred. Unity's engine diagnostics defaults should be reviewed in the shipping build along with Play data-safety declarations.
- Build a release AAB with the correct existing signing identity if continuing the old store listing; upload only after store settings, privacy disclosures, asset rights, content rating and pre-launch testing are complete.

## Guidance used

Unity skill references: `art-direction`, `camera-rigs`, `game-feel`, `vfx`, `game-audio`, `game-shell`, `visual-scorecard`; `new-unity-project`, `unity-cli`, `unity-pipeline`, `ui`, `ui-ugui`, `unity-package-management`. The 3D lighting/character floors do not apply to this geometric 2D port; its original fixed central viewpoint is retained. Audio uses one shared Music/SFX gain layer rather than an authored mixer asset. Cloud identity/live-service floors are skipped for the offline single-player scope.

API references checked against Unity 6.6: [Android player settings](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityeditor/playersettings/android), [delta time](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/time/deltatime), [build pipeline](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityeditor/buildpipeline/buildplayer), [safe area](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/screen/safearea), [audio](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/audiosource/playoneshot), and the installed uGUI 2.6 TMP importer source/API.
