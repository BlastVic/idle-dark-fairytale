using UnityEngine;
using Spine.Unity;

namespace RedHoodPrototype {
    // Standalone animation viewer: deliberately has no inventory/save/combat dependencies.
    public sealed class RedHoodAnimationPreview : MonoBehaviour {
        public SkeletonAnimation character;
        [Range(0.1f, 2f)] public float speed = 1f;
        public bool autoAttack = true;
        public bool paused;
        private string selected = "Attack1";
        private float nextAttack = -1f;
        private int hitCount;
        private float flash;
        private bool alternate;
        private float timeline;
        private bool inspecting;
        private Spine.TrackEntry Current { get { return character && character.AnimationState != null ? character.AnimationState.GetCurrent(0) : null; } }
        private readonly string[] animations = { "Idle1", "Attack1", "Attack2", "Hit", "Skill1", "Death1", "Victory", "Rebirth" };

        void Start() {
            if (!character) { enabled = false; return; }
            character.Initialize(false);
            character.AnimationState.Event += OnSpineEvent;
            Play(autoAttack ? "Attack1" : "Idle1");
        }

        void OnDestroy() {
            if (character && character.AnimationState != null)
                character.AnimationState.Event -= OnSpineEvent;
        }

        void OnSpineEvent(Spine.TrackEntry entry, Spine.Event e) {
            if (inspecting || entry != Current) return;
            if (e.Data.Name == "OnHit") { hitCount++; flash = 0.18f; }
            if (e.Data.Name == "OnComplete" && autoAttack) nextAttack = 0.2f;
        }

        public void Play(string animation) {
            if (character.Skeleton.Data.FindAnimation(animation) == null) return;
            selected = animation;
            nextAttack = -1f;
            timeline = 0;
            inspecting = false;
            paused = false;
            character.timeScale = speed;
            character.AnimationState.SetAnimation(0, animation, animation == "Idle1");
            character.Update(0);
        }

        void Update() {
            if (!character || character.AnimationState == null) return;
            character.timeScale = paused ? 0 : speed;
            if (!inspecting && Current != null) timeline = Current.AnimationTime;
            if (paused) return;
            flash = Mathf.Max(0, flash - Time.deltaTime);
            if (autoAttack && nextAttack >= 0) {
                nextAttack -= Time.deltaTime * speed;
                if (nextAttack <= 0) {
                    alternate = !alternate;
                    Play(alternate ? "Attack2" : "Attack1");
                }
            }
        }

        public void Seek(float seconds) {
            autoAttack = false;
            paused = true;
            inspecting = true;
            character.timeScale = 0;
            character.AnimationState.ClearTracks();
            character.Skeleton.SetToSetupPose();
            var entry = character.AnimationState.SetAnimation(0, selected, false);
            timeline = Mathf.Clamp(seconds, 0, entry.Animation.Duration);
            entry.TrackTime = timeline;
            entry.AnimationLast = timeline;
            character.AnimationState.Apply(character.Skeleton);
            character.Skeleton.UpdateWorldTransform();
            character.LateUpdate();
        }

        void OnGUI() {
            if (!character || character.AnimationState == null) return;
            float scale = Mathf.Min(Screen.width / 540f, Screen.height / 960f);
            var matrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 540 * scale) / 2, 0), Quaternion.identity, Vector3.one * scale);
            GUILayout.BeginArea(new Rect(18, 16, 504, 218), GUI.skin.box);
            GUILayout.Label("RED HOOD / SPINE ANIMATION LAB");
            GUILayout.Label("Reference side view / new rig and scythe animation timelines");
            GUILayout.Label("Playing: " + selected + "    OnHit events: " + hitCount);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(paused ? "Resume" : "Pause", GUILayout.Height(30))) { paused = !paused; inspecting = false; }
            if (GUILayout.Button(autoAttack ? "Auto attack: ON" : "Auto attack: OFF", GUILayout.Height(30))) {
                autoAttack = !autoAttack;
                if (autoAttack) Play("Attack1");
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Speed " + speed.ToString("0.0") + "x", GUILayout.Width(95));
            speed = GUILayout.HorizontalSlider(speed, 0.1f, 2f);
            GUILayout.EndHorizontal();
            if (Current != null) {
                float duration = Current.Animation.Duration;
                GUILayout.Label("Time: " + timeline.ToString("0.00") + " / " + duration.ToString("0.00") + " s");
                float next = GUILayout.HorizontalSlider(timeline, 0, duration);
                if (Mathf.Abs(next - timeline) > 0.001f) Seek(next);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("< Frame")) Seek(timeline - 1f / 60f);
                if (GUILayout.Button("Frame >")) Seek(timeline + 1f / 60f);
                if (GUILayout.Button("Restart")) { Play(selected); paused = false; }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndArea();
            if (flash > 0) GUI.Label(new Rect(235, 180, 150, 40), "HIT!");
            GUILayout.BeginArea(new Rect(18, 775, 504, 170), GUI.skin.box);
            GUILayout.Label("Select an animation (disables auto attack)");
            for (int row = 0; row < 2; row++) {
                GUILayout.BeginHorizontal();
                for (int col = 0; col < 4; col++) {
                    string name = animations[row * 4 + col];
                    if (GUILayout.Button(name, GUILayout.Height(42))) { autoAttack = false; Play(name); }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.Label("Independent cape and lantern sway / wrist IK grip");
            GUILayout.EndArea();
            GUI.matrix = matrix;
        }
    }
}
