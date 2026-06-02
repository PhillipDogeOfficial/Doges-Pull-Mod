using BepInEx;
using BepInEx.Configuration;
using DogesChecker;
using GorillaNetworking;
using HarmonyLib;
using Photon.Pun;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace PhillipFreecam
{
    [BepInPlugin("com.doges.pullmod", "doges pullmod", "1.0.0")]
    public class Main : BaseUnityPlugin
    {
        bool isMenuCreated;
        GameObject menuObj;
        List<GameObject> btnObjs = new List<GameObject>();
        public static Main instance;

        public ConfigEntry<bool> pullmodEnabled;

        float lastPressTime = 0f;
        float doublePressWindow = 0.3f;
        int pressCount = 0;
        bool lastPrimaryState = false;

        void Awake()
        {
            instance = this;
            pullmodEnabled = Config.Bind("Settings", "Pull Mod", false, "Doge is the best");
            pullmodEnabled.Value = false;
            Harmony harmony = new Harmony("com.dogespullmod.1.0.0");
            harmony.PatchAll();
        }

        void Update()
        {
            if (ControllerInputPoller.instance == null)
                return;

            if (GorillaLocomotion.GTPlayer.Instance == null)
                return;

            Mods.leftGrabFloat = ControllerInputPoller.instance.leftControllerGripFloat;
            Mods.rightGrabFloat = ControllerInputPoller.instance.rightControllerGripFloat;

            HandleDoublePress();

            if (pullmodEnabled.Value)
                Mods.PullModUpdate();

            AnimateMenuColor();
        }

        void HandleDoublePress()
        {
            bool current = ControllerInputPoller.instance.leftControllerPrimaryButton;

            if (current && !lastPrimaryState)
            {
                if (Time.time - lastPressTime <= doublePressWindow)
                {
                    pressCount++;
                    if (pressCount == 2)
                    {
                        if (!isMenuCreated)
                            CreateMenu();
                        else
                            DestroyMenu();
                        pressCount = 0;
                    }
                }
                else
                {
                    pressCount = 1;
                }
                lastPressTime = Time.time;
            }

            lastPrimaryState = current;
        }

        void AnimateMenuColor()
        {
            if (menuObj == null)
                return;

            float t = (Mathf.Sin(Time.time * 2f) + 1f) / 2f;
            Color c = Color.Lerp(Color.yellow, Color.white, t);
            menuObj.GetComponent<Renderer>().material.color = c;
        }

        void CreateMenu()
        {
            isMenuCreated = true;

            var player = GorillaLocomotion.GTPlayer.Instance;

            menuObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            menuObj.transform.parent = player.LeftHand.controllerTransform;
            menuObj.transform.localPosition = Vector3.zero;
            menuObj.transform.localRotation = Quaternion.identity;
            menuObj.transform.localScale = Vector3.zero;

            Destroy(menuObj.GetComponent<Rigidbody>());
            Destroy(menuObj.GetComponent<Collider>());

            var rend = menuObj.GetComponent<Renderer>();
            rend.material.shader = Shader.Find("GorillaTag/UberShader");

            AddButton(0.15f, "Pull Mod");
            AddButton(0.05f, "+");
            AddButton(0.05f, "-");

            StartCoroutine(AnimateOpen(menuObj, new Vector3(0.03f, 0.3f, 0.45f)));
        }

        void DestroyMenu()
        {
            isMenuCreated = false;
            StartCoroutine(AnimateClose(menuObj));
            DestroyAllButtons();
            Config.Save();
        }

        System.Collections.IEnumerator AnimateOpen(GameObject obj, Vector3 targetScale)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 10f;
                obj.transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, t);
                yield return null;
            }
            obj.transform.localScale = targetScale;
        }

        System.Collections.IEnumerator AnimateClose(GameObject obj)
        {
            Vector3 startScale = obj.transform.localScale;
            float t = 0f;

            while (t < 1f)
            {
                t += Time.deltaTime * 10f;
                if (obj != null)
                    obj.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                yield return null;
            }

            if (obj != null)
                Destroy(obj);
        }

        void AddButton(float zOffset, string btnName)
        {
            GameObject btnObj = GameObject.CreatePrimitive(PrimitiveType.Cube);

            var player = GorillaLocomotion.GTPlayer.Instance;

            var follow = btnObj.AddComponent<FollowMenu>();
            follow.target = player.LeftHand.controllerTransform;

            float ySideOffset = 0f;

            if (btnName == "+")
                ySideOffset = 0.08f;
            else if (btnName == "-")
                ySideOffset = -0.08f;

            follow.position = new Vector3(0.015f, ySideOffset, zOffset);
            follow.rotation = Quaternion.identity;

            Vector3 buttonSize = new Vector3(0.03f, 0.2f, 0.04f);

            if (btnName == "+" || btnName == "-")
                buttonSize = new Vector3(0.02f, 0.07f, 0.04f);

            btnObj.transform.localScale = buttonSize;

            var rend = btnObj.GetComponent<Renderer>();
            rend.material.shader = Shader.Find("GorillaTag/UberShader");
            rend.material.color = Color.black;

            btnObj.GetComponent<Collider>().isTrigger = true;
            btnObj.layer = 18;

            var trigger = btnObj.AddComponent<ButtonTrigger>();
            trigger.btnIdentifer = btnName;

            var textObject = new GameObject("ButtonLabel");
            textObject.transform.SetParent(btnObj.transform);
            textObject.transform.localPosition = new Vector3(0.55f, 0f, 0f);
            textObject.transform.localRotation = Quaternion.Euler(0f, -90f, -90f);

            var text = textObject.AddComponent<TextMeshPro>();
            text.text = btnName;
            text.fontSize = 100;
            text.alignment = TextAlignmentOptions.Center;
            text.rectTransform.sizeDelta = new Vector3(50f, 40f);
            text.transform.localScale = new Vector3(0.01f, 0.1f, 0.3f);
            text.color = Color.white;

            btnObjs.Add(btnObj);
        }

        void DestroyAllButtons()
        {
            foreach (GameObject btnObj in btnObjs)
                Destroy(btnObj);
            btnObjs.Clear();
        }
    }

    public class FollowMenu : MonoBehaviour
    {
        public Transform target;
        public Vector3 position;
        public Quaternion rotation;

        void LateUpdate()
        {
            transform.position = target.TransformPoint(position);
            transform.rotation = target.rotation * rotation;
        }
    }

    public class ButtonTrigger : GorillaPressableButton
    {
        public string btnIdentifer;
        float lastPressTime = 0f;
        float cooldown = 1f;

        public override void ButtonActivationWithHand(bool isLeftHand)
        {
            base.ButtonActivationWithHand(isLeftHand);

            if (!isLeftHand)
            {
                if (btnIdentifer == "Pull Mod")
                {
                    if (Time.time - lastPressTime < cooldown)
                        return;
                    lastPressTime = Time.time;
                }

                switch (btnIdentifer)
                {
                    case "Pull Mod":
                        Main.instance.pullmodEnabled.Value = !Main.instance.pullmodEnabled.Value;
                        break;

                    case "+":
                        Mods.pullPower += 0.1f;
                        break;

                    case "-":
                        Mods.pullPower -= 0.1f;
                        if (Mods.pullPower < 0f) Mods.pullPower = 0f;
                        break;
                }

                Main.instance.Config.Save();
            }
        }
    }
}
