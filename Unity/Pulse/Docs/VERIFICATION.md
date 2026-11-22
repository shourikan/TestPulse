# Port verification — 14 November 2026

Playable Unity single-player port, with an Android test APK. The Processing source remains unchanged. Bluetooth versus is deferred by agreement.

## Build and rules

- Unity 6000.6.9f1, 2D URP; Android ARM64 IL2CPP, portrait, API26 minimum / API36 target. The generated manifest requires OpenGL ES 3.1.
- Current build after the motion-control revision: **Succeeded, zero errors, three warnings**. The warnings concern large TextMeshPro methods receiving separate IL2CPP source files. Compilation and simulated-sensor Play Mode checks completed without errors. `AndroidBuildReport.json` records the build summary. Its aggregate output size includes associated build artifacts; the APK itself is **30,819,408 bytes**. Android accepted the in-place update; after unlocking, title/gameplay/pause and the active gravity sensor were verified on the phone.
- APK: [Pulse.apk](../Builds/Android/Pulse.apk). Debug signing is for local testing. No release keystore or store upload was used.
- Original APK validation: `apksigner verify --verbose` passed, APK Signature Scheme v2; `zipalign -c -P 16 4` passed. All seven original APK native libraries had minimum ELF LOAD alignment 16,384 bytes. `ApkVerification.json` and `NativeAlignment.json` describe that original artifact. Android accepted the revised APK as an in-place update.
- Compiled manifest confirms package `spetko.ondrej.TestPulse`, version 1.0.0/code 2, ARM64 only, minimum SDK26 and target SDK36. Permissions: Internet, vibration, and AndroidX's app-specific protected receiver permission. No Bluetooth permissions.
- **29/29 EditMode tests passed** under the Android configuration; see `TestResults.json`. Coverage includes the original rules checks, both 60-degree steering extremes, absence of a second simulation steering lag, wake behavior, and motion-control saturation/return/sensitivity/calibration/filtering checks.
- Play checks confirmed advancing frames, title → playing → paused → ended → retry, frozen scoring while paused, clean run counters on retry, active music, and zero music output when the Music gain is zero. Review-generated scores/run counts were restored to their initial zero values.
- Material/font/event-system audit passed: no missing/default sprite materials, no missing fonts, exactly one EventSystem. Ten effect bursts retained the same **437 SpriteRenderers**. The busy capture had **102 active pooled particles**.
- No current compile or console errors. Play Mode stopped; run-in-background and editor auto-tick overrides returned to off.

## Visual review

Reviewed portrait frames at 540×960 and 540×1170. Safe-area handling is implemented; cutouts, navigation bars, motion sensors and thermal behavior still require physical devices.

The first draft failed silhouette/outline clarity because of an alpha-generation error. The fix pass corrected the antialiased alpha ramps, repaired slider handle sizing, and stacked concurrent pickup labels. Final six-axis board, using the skill's 2D outline equivalent for lighting depth:

palette discipline: 4/5 — original mint/violet/gold/blue/plum roles trace to PulsePalette.

style uniformity: 3/5 — geometric cores, orbital outlines and consistent typography throughout.

lighting depth / outline discipline: 3/5 — critical pieces share crisp silhouettes and orbital outlines, with subordinate halos.

silhouette: 3/5 — stationary mint player reads clearly; hazard, gold and shield roles have distinct cores.

UI intentionality: 3/5 — aligned portrait HUD, styled title/pause/results/settings, readable controls and circular slider handles.

feedback visibility: 3/5 — the busy frame shows shield field, particles, score changes and separated event labels.

Draft scores before the fix pass: palette 4, style 2, outline 1, silhouette 1, UI 3, feedback 2. These are a simple arcade/jam-quality assessment, not a claim of commercial release certification.

Captures are local review artifacts in `Screenshots/` (ignored by git): `title-final.png`, `settings-final.png`, `settings-tall.png`, `pause-final.png`, `results-final.png`, `player-closeup.png`, `gameplay-final.png`. The busy field is deliberately arranged by the editor-only review helper to stress concurrent feedback; it is not a difficulty benchmark.

## Scope and side effects

Applied art direction, feel, VFX, audio, shell and visual review. Skill references read: `art-direction`, `camera-rigs`, `game-feel`, `vfx`, `game-audio`, `game-shell`, `visual-scorecard`; plus the Unity project/CLI/Pipeline, package-management, uGUI and Computer Use guides. API pages and the 2D/3D guidance gaps are recorded in `ANDROID_QA.md`. The original fixed-center camera is retained; no follow rig is added. 3D lighting/character floors and cloud/live-service setup do not apply to this offline geometric game. Audio uses the documented shared gain layer instead of an authored AudioMixer.

Created the nested project, scripts, tests, scene, five procedural sprites, unlit material, two adaptive-icon layers, local save/settings handling, build menus and docs. Reused the original six audio files and legacy launcher icon. Imported TMP Essential Resources. The Unity template supplies URP 17.6, Input System 1.20.1, uGUI 2.6, Test Framework 1.8 and its standard 2D/editor packages; project creation installed Pipeline 1.0. No cloud project, account service, ads, purchases or multiplayer was added. Template diagnostics defaults were retained and must be reviewed for the shipping release. Generated caches, screenshots, builds and signing files are ignored. Changes are left uncommitted for review.

Release gates: full physical-phone controls/interruption/performance testing, final permission/diagnostics review, asset distribution rights, the correct release signing identity, and store listing/declarations. See `ANDROID_QA.md`. A subsequent USB device smoke test installed and launched this APK successfully on a Samsung SM-G991B running Android 15; see [DEVICE_TEST.md](DEVICE_TEST.md) for results and remaining checks.


## Marketplace polish — 22 November 2026

- All 31 Edit Mode tests passed, including the gentler first-run placement and the shield-consumption feedback event. Play Mode verified the first meaningful steer dismisses the primer, a held drag can cross the lower HUD, a second finger can tap Pulse while steering, and release recenters the heading. Simulated gravity input verified Tilt and the 60-degree cap.
- The title, settings, first run, ready and active Pulse, pause, and results were reviewed in [portrait captures](../Screenshots/PolishReview/contact-sheet.png). Settings was also checked at 540 × 960. The captured charge and pickup situations were staged. Personal best and run count were reloaded before restoring the original editor preferences.
- Material, font, EventSystem, gain, and pooling checks passed with 372 preallocated sprite renderers. The visual check scored palette, style, outlines, silhouette, UI, and feedback at 4/5 each. The HUD has separate collecting, ready, and active states, and actor silhouettes stay distinguishable at gameplay size.
- Review used the Unity craft references for art direction, game shell, game feel, VFX and visual scoring, and the Pipeline capture, scripts and lifecycle references. The UI uses the existing uGUI Canvas Scaler and RectTransformUtility. The countdown arc uses the Line Renderer. [Canvas Scaler](https://docs.unity3d.com/Packages/com.unity.ugui@2.6/manual/script-CanvasScaler.html) · [RectTransformUtility](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/RectTransformUtility.RectangleContainsScreenPoint.html) · [Line Renderer](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/LineRenderer.html)
- The ARM64 [Android test APK](../Builds/Android/Pulse-Polish.apk) built with zero errors and three TextMeshPro IL2CPP compilation advisories. APK Signature Scheme v2 and 16 KB ZIP alignment passed verification. Physical phone testing is pending.

## Shield gesture and collision feedback

- Android documents double-tap as a common gesture. Xbox’s input guidance recommends alternatives to multi-point and rapid inputs. Pulse accepts two short releases in the same area of the playfield once charged and also keeps its single-tap HUD action. [Android gesture detection](https://developer.android.com/develop/ui/views/touch-and-input/gestures/detector) · [Xbox Accessibility Guideline 107](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/107)
- All 32 Edit Mode tests passed. The added regression verifies steady post-run motion at the final heading, fixed run totals and no post-run collision events.
- Play Mode checks used injected touchscreen events across player frames. Pre-charge taps, a slow pair, a quick drag and a tap carried across pause left Pulse charged. The second tap release and the single-tap HUD button each activated it. The inner arc drained within a fixed outline. Pause and settings stars moved while Reduce motion froze them. Shield impact and death splashes persisted, and Retry cleared the effect pool. The final check reported zero console errors.
- Reviewed [six portrait captures](../Screenshots/FeedbackReview/contact-sheet.png), including staged Pulse and death scenes. The new [Android test APK](../Builds/Android/Pulse-Feedback.apk) built with zero errors and three TextMeshPro IL2CPP advisories. Its v2 signature and 16 KB ZIP alignment passed. The Galaxy S21 accepted the in-place update over USB.
