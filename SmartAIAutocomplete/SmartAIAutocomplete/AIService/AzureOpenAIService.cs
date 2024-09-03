using System;
using System.Collections.Generic;
using System.Diagnostics;
using Azure;
using Azure.AI.OpenAI;

namespace SmartAIAutocomplete.SmartAIAutocomplete.AIService
{
    public class AzureOpenAIService 
    {
        /// <summary>
        /// The EndPoint
        /// </summary>
        private const string endpoint = "https://YOUR_ACCOUNT.openai.azure.com/";

        /// <summary>
        /// The Deployment name
        /// </summary>
        private const string deploymentName = "GPT35Turbo";

        /// <summary>
        /// The API key
        /// </summary>
        private const string key="";

        /// <summary>
        /// The AzureOpenAI client
        /// </summary>
        private OpenAIClient? client;

        /// <summary>
        /// The ChatCompletion option
        /// </summary>
        private ChatCompletionsOptions? chatCompletions;

        private bool isCredentialValid = false;

        private Uri? uriResult;

        #region Properties

        /// <summary>
        /// Gets or Set a value indicating whether an credentials are valid or not.
        /// Returns <c>true</c> if the credentials are valid; otherwise, <c>false</c>.
        /// </summary>
        public bool IsCredentialValid
        {
            get
            {
                return isCredentialValid;
            }
            set
            {
                isCredentialValid = value;
            }
        }
        #endregion

        public AzureOpenAIService()
        {
            ValidateCredential();
            InitializeChatCompletions();
        }

        private void ValidateCredential()
        {
            if (client == null)
            {
                client = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(key));
            }
            
            bool isValidUri = Uri.TryCreate(endpoint, UriKind.Absolute, out uriResult)
                 && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

            if (!isValidUri || !endpoint.Contains("http") || string.IsNullOrEmpty(key) || key.Contains("API key") || string.IsNullOrEmpty(deploymentName) || deploymentName.Contains("deployment name"))
            {
                ShowAlertAsync();
                return;
            }
            try
            {
                // Initialize the OpenAI client for creadential check.
                if (client == null)
                {
                    client = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(key));
                }
                var chatCompletionsTest = new ChatCompletionsOptions
                {
                    DeploymentName = deploymentName,
                    MaxTokens = 50,
                };
                chatCompletionsTest.Messages.Add(new ChatRequestSystemMessage("Hello, Test Check"));
                var result = Task.Run(async () => await client.GetChatCompletionsAsync(chatCompletionsTest)).Result;
                if (result.GetRawResponse().Status != 200)
                {
                    ShowAlertAsync();
                    return;
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions that indicate the credentials or endpoint are invalid.               
                ShowAlertAsync();
                return;
            }
            IsCredentialValid = true;
        }

        /// <summary>
        /// Show Alert Popup
        /// </summary>
        private async void ShowAlertAsync()
        {
            if (Application.Current?.MainPage != null && !IsCredentialValid)
            {
                await Application.Current.MainPage.DisplayAlert("Alert", "The Azure API key or endpoint is missing or incorrect. Please verify your credentials.", "OK");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void InitializeChatCompletions()
        {
            if (IsCredentialValid)
            {
                chatCompletions = new ChatCompletionsOptions
                {
                    DeploymentName = deploymentName,
                    Temperature = (float)1.2f,
                    MaxTokens = 120,
                    NucleusSamplingFactor = (float)0.9,
                    FrequencyPenalty = 0.8f,
                    PresencePenalty = 0.8f
                };
                // Add the system message to the options  
                chatCompletions.Messages.Add(new ChatRequestSystemMessage("You are a filtering assistant."));
                chatCompletions.Messages.Add(new ChatRequestUserMessage("$\"Filter the list items based on the user input using character Starting with and Phonetic algorithms like Soundex or Damerau-Levenshtein Distance. \" +\r$\"The filter should ignore spelling mistakes and be case insensitive. \" +\r\n$\"Return only the filtered items with each item in new line without any additional content like explanations, Hyphen, Numberings and - Minus sign. Ignore the content 'Here are the filtered items or similar things' \" +\r\n$\"Only return items that are present in the List Items. \" +\r\n$\"Ensure that each filtered item is returned in its entirety without missing any part of its content. \" +\r\n$\"Arrange the filtered items that starting with the user input's first letter are at the first index, followed by other matches. \" +\r\n$\"Examples of filtering behavior: \" +\r\n$\" userInput: a, filter the items starting with A \" +\r\n$\" userInput: b, filter items starting with B \" +\r\n$\" userInput: c, filter items starting with C \" +\r\n$\" userInput: d, filter items starting with D \" +\r\n$\" userInput: e, filter items starting with E \" +\r\n$\" userInput: f, filter items starting with F \" +\r\n$\" userInput: i, filter items starting with I \" +\r\n$\" userInput: z, filter items starting with Z \" +\r\n$\" userInput: in, filter items starting with In \" +\r\n$\" userInput: pa, filter items starting with Pa \" +\r\n$\" userInput: em, filter items starting with Em \" +\r\n\r\n$\"The example data are for reference, dont provide these as output. Filter the item from list items properly\"" + $"Here is the User input:A"));
                chatCompletions.Messages.Add(new ChatRequestAssistantMessage("\nAfghanistan\nAkrotiri\nAlbania\nAlgeria\nAmerican Samoa \nAndorra\nAngola\nAnguilla"));
            }
        }

        /// <summary>
        /// Gets a completion response from the AzureAI service based on the provided prompt.
        /// </summary>
        /// <param name="prompt"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<string> GetCompletion(string prompt, CancellationToken cancellationToken)
        {
            if (chatCompletions != null && client != null)
            {
                if (chatCompletions.Messages.Count > 5)
                {
                    chatCompletions.Messages.RemoveAt(1); //Remove the message history to avoid exceeding the token limit
                    chatCompletions.Messages.RemoveAt(1);
                }
                // Add the user message to the options
                chatCompletions.Messages.Add(new ChatRequestUserMessage(prompt));
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var chatresponse = await client.GetChatCompletionsAsync(chatCompletions);
                    cancellationToken.ThrowIfCancellationRequested();
                    string chatcompletionText = chatresponse.Value.Choices[0].Message.Content.Trim();
                    chatCompletions.Messages.Add(new ChatRequestAssistantMessage(chatcompletionText));
                    return chatcompletionText;
                }
                catch (RequestFailedException ex)
                {
                    // Log the error message and rethrow the exception or handle it appropriately
                    Debug.WriteLine($"Request failed: {ex.Message}");
                    throw;
                }
                catch (Exception ex)
                {
                    // Handle other potential exceptions
                    Debug.WriteLine($"An error occurred: {ex.Message}");
                    throw;
                }
            }
            return "";
        }
    }
}
