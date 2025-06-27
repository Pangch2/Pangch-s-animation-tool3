using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace GameSystem
{
    public class LogConsole : MonoBehaviour
    {
        public static LogConsole instance;
        public TextMeshProUGUI[] text;

        private int _index;

        private void Awake()
        {
            if (instance != null)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
        }

        public void Log(object message, Color co)
        {
            text[_index].text = message.ToString();
            text[_index].color = co;
            // StartCoroutine(TextCoroutine(text[_index]));
            TextCoroutine(text[_index], this.GetCancellationTokenOnDestroy()).Forget();

            _index = (_index + 1) % text.Length;
        }

        public void Log(object message)
        {
            Log(message, Color.white);
        }

        private async UniTask TextCoroutine(TextMeshProUGUI txt, CancellationToken token)
        {
            try
            {
                txt.gameObject.SetActive(true);
                await UniTask.Delay(TimeSpan.FromSeconds(5f), cancellationToken: token);
                txt.color = Color.clear;
                txt.gameObject.SetActive(false);
            }
            catch (OperationCanceledException)
            {
                // Handle cancellation if needed
            }
        }

        void OnDestroy()
        {
            instance = null;
        }
    }
}
