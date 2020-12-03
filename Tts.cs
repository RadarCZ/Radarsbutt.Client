using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Newtonsoft.Json;
using Radarsbutt.Client.Types;

namespace Radarsbutt.Client
{
    public class Tts
    {
        private readonly SpeechConfig _config;
        private readonly List<Voice> _voices;

        private Tts(string token, List<Voice> voices)
        {
            _voices = voices;
            _config = SpeechConfig.FromAuthorizationToken(token, "westeurope");
            _config.SpeechSynthesisVoiceName = "Microsoft Server Speech Text to Speech Voice (en-GB, George)";
            _config.SpeechRecognitionLanguage = "en-GB";
        }

        public async Task ProcessTts(List<string> textToProcess)
        {
            try
            {
                if (textToProcess[0] != "dump")
                {
                    throw new ArgumentException("Unsupported custom voice or not actually argument.");
                }

                var customVoiceEndpoint =
                    Environment.GetEnvironmentVariable($"{textToProcess[0].ToUpperInvariant()}_TTS_ENDPOINT");
                var subscriptionKey = Environment.GetEnvironmentVariable("AZURE_TTS_SUBSCRIPTION_KEY");

                if (string.IsNullOrEmpty(customVoiceEndpoint) || string.IsNullOrEmpty(subscriptionKey))
                {
                    var message =
                        $"Cannot initialize custom voice '{textToProcess[0]}' - Custom voice endpoint and/or subscription key is missing";
                    await Console.Error.WriteLineAsync(message);
                    throw new ArgumentException(message);
                }

                SpeechConfig customConfig = SpeechConfig.FromEndpoint(new Uri(customVoiceEndpoint), subscriptionKey);
                customConfig.SpeechSynthesisLanguage = "en-US";
                customConfig.SpeechSynthesisVoiceName = "DumpModel02";
                var finalText = new List<string>(textToProcess);
                finalText.RemoveAt(0);
                using var synth = new SpeechSynthesizer(customConfig);
                await synth.SpeakTextAsync(string.Join(" ", finalText));
            }
            catch (Exception)
            {
                Voice voiceToUse =
                    _voices.SingleOrDefault(voice => voice.DisplayName.ToLowerInvariant() == textToProcess[0]);
                var finalText = new List<string>(textToProcess);
                if (voiceToUse != default(Voice))
                {
                    _config.SpeechSynthesisLanguage = voiceToUse.Locale;
                    _config.SpeechSynthesisVoiceName = voiceToUse.Name;
                    finalText.RemoveAt(0);
                }

                using var synth = new SpeechSynthesizer(_config);
                await synth.SpeakTextAsync(string.Join(" ", finalText));

                _config.SpeechSynthesisLanguage = "";
                _config.SpeechSynthesisVoiceName = "";
            }
        }

        private static async Task<string> RenewToken()
        {
            var subKey = Environment.GetEnvironmentVariable("AZURE_TTS_SUBSCRIPTION_KEY");

            if (string.IsNullOrEmpty(subKey))
            {
                throw new ArgumentException("Azure TTS Subscription key is missing.");
            }

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subKey);
            HttpResponseMessage tokenResponse =
                await client.PostAsync("https://westeurope.api.cognitive.microsoft.com/sts/v1.0/issuetoken", null);
            if (tokenResponse.IsSuccessStatusCode)
            {
                return await tokenResponse.Content.ReadAsStringAsync();
            }
            throw new WebException("Could not get access token. Possibly expired Subscription key or azure servers on vacation.");
        }

        public static async Task<Tts> Init()
        {
            try
            {
                var token = await RenewToken();
                var subKey = Environment.GetEnvironmentVariable("AZURE_TTS_SUBSCRIPTION_KEY");

                if (string.IsNullOrEmpty(subKey))
                {
                    throw new ArgumentException("Azure TTS Subscription key is missing.");
                }

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subKey);
                client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse($"Bearer ${token}");
                var voicesResponse =
                    await client.GetStringAsync(
                        "https://westeurope.tts.speech.microsoft.com/cognitiveservices/voices/list");
                var voices = JsonConvert.DeserializeObject<List<Voice>>(voicesResponse);
                return new Tts(token, voices);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }
    }
}