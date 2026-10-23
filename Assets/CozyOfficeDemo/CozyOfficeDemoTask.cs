using UnityEngine;
using UnityEngine.InputSystem;

namespace CozyOfficeDemo
{
    public class CozyOfficeDemoTask : MonoBehaviour
    {
        public Transform player;
        public Transform printer;
        public Transform desk;
        public GameObject printout;
        public float interactionDistance = 2.2f;
        private int stage;
        private string message = "Welcome aboard. The printer has your first assignment.";
        private GUIStyle title, body, prompt;

        private void Update()
        {
            if (Keyboard.current == null || player == null) return;
            if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                stage = 0;
                message = "Another fresh start. The printer is calling.";
                if (printout != null) printout.SetActive(true);
            }
            if (!Keyboard.current.eKey.wasPressedThisFrame) return;
            if (stage == 0 && Near(printer))
            {
                stage = 1;
                message = "Printout collected. Apparently this email needed to be paper.";
                if (printout != null) printout.SetActive(false);
            }
            else if (stage == 1 && Near(desk))
            {
                stage = 2;
                message = "Report submitted. Only three meetings until lunch.";
            }
        }

        private bool Near(Transform target)
        {
            if (player == null || target == null) return false;
            Vector3 delta = player.position - target.position;
            delta.y = 0;
            return delta.sqrMagnitude <= interactionDistance * interactionDistance;
        }

        private void OnGUI()
        {
            if (title == null)
            {
                title = new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold, wordWrap = true };
                body = new GUIStyle(GUI.skin.label) { fontSize = 18, wordWrap = true };
                prompt = new GUIStyle(body) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                title.normal.textColor = body.normal.textColor = prompt.normal.textColor = Color.white;
            }
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / 1280f, Screen.height / 720f, 1));
            GUI.Box(new Rect(24, 24, 520, 170), GUIContent.none);
            GUI.Label(new Rect(42, 34, 480, 44), "COZY OFFICE | A fresh start", title);
            string objective = stage == 0 ? "1 / 2  Collect your printout at PRINT & PANIC" : stage == 1 ? "2 / 2  Submit the report at YOUR DESK" : "DAY ONE COMPLETE  |  Task finished!";
            GUI.Label(new Rect(42, 85, 480, 55), objective, body);
            GUI.Label(new Rect(42, 148, 480, 34), "WASD: Move     E: Interact     R: Reset tasks", body);
            GUI.Box(new Rect(210, 598, 860, 96), GUIContent.none);
            GUI.Label(new Rect(232, 610, 816, 68), message, prompt);
            string action = stage == 0 && Near(printer) ? "[ E ] Collect printout" : stage == 1 && Near(desk) ? "[ E ] Submit report" : "";
            if (action.Length > 0)
            {
                GUI.Box(new Rect(420, 520, 440, 52), GUIContent.none);
                GUI.Label(new Rect(428, 520, 424, 52), action, prompt);
            }
            GUI.matrix = previous;
        }
    }
}
