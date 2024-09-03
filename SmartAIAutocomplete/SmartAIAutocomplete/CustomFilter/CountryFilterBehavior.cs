using SmartAIAutocomplete.SmartAIAutocomplete.AIService;
using SmartAIAutocomplete.SmartAIAutocomplete.Model;
using Syncfusion.Maui.Inputs;
using System.Collections;
using System.Collections.ObjectModel;

namespace SmartAIAutocomplete.SmartAIAutocomplete.CountryFilterBehavior
{
    public class CountryFilterBehavior : IAutocompleteFilterBehavior
    {
        private readonly AzureOpenAIService _azureAIService;
        public ObservableCollection<CountryModel> Countries { get; set; }
        public ObservableCollection<CountryModel> FilteredCountries { get; set; } = new ObservableCollection<CountryModel>();
        private CancellationTokenSource? _cancellationTokenSource;

        public CountryFilterBehavior()
        {
            _azureAIService = new AzureOpenAIService();
            Countries = new ObservableCollection<CountryModel>();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        

        /// <summary>
        ///  Finds matching items using the typed text
        /// </summary>
        /// <param name="source"></param>
        /// <param name="filterInfo"></param>
        /// <returns></returns>
        public async Task<object?> GetMatchingItemsAsync(SfAutocomplete source, AutocompleteFilterInfo filterInfo)
        {
            //If crendential is not valid the filtering data shows as empty
            if (!_azureAIService.IsCredentialValid)
            {
                FilteredCountries.Clear();
                return await Task.FromResult(FilteredCountries);
            }

            if (string.IsNullOrEmpty(filterInfo.Text))
            {
                _cancellationTokenSource?.Cancel();
                FilteredCountries.Clear();
                return await Task.FromResult(FilteredCountries);
            }

            Countries = (ObservableCollection<CountryModel>)source.ItemsSource;


            string listItems = string.Join(", ", Countries!.Select(c => c.Name));

            // Join the first five items with newline characters for demo output template for AI           
            string outputTemplate = string.Join("\n", Countries.Take(5).Select(c => c.Name));

            //The cancellationToken was used for cancelling the API request if user types continuously       
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;

            //Passing the User Input, ItemsSource, Reference output and CancellationToken
            var filterCountries = await FilterCountriesUsingAzureAI(filterInfo.Text, listItems, outputTemplate, cancellationToken);

            return await Task.FromResult(filterCountries);
        }

        /// <summary>
        /// Filters country names based on user input using Azure AI.
        /// </summary>
        /// <param name="userInput"></param>
        /// <param name="itemsList"></param>
        /// <param name="outputTemplate"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<ObservableCollection<CountryModel>> FilterCountriesUsingAzureAI(string userInput, string itemsList, string outputTemplate, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(userInput))
            {
                var prompt = $"Filter the list items based on the user input using character Starting with and Phonetic algorithms like Soundex or Damerau-Levenshtein Distance. " +
                            $"The filter should ignore spelling mistakes and be case insensitive. " +
                            $"Return only the filtered items with each item in new line without any additional content like explanations, Hyphen, Numberings and - Minus sign. Ignore the content 'Here are the filtered items or similar things' " +
                            $"Only return items that are present in the List Items. " +
                            $"Ensure that each filtered item is returned in its entirety without missing any part of its content. " +
                            $"Arrange the filtered items that starting with the user input's first letter are at the first index, followed by other matches. " +
                            $"Examples of filtering behavior: " +
                            $"The example data are for reference, dont provide these as output. Filter the item from list items properly" +
                            $"Here is the User input: {userInput}, " +
                            $"List of Items: {itemsList}" +
                            $"If no items found, return \"Empty\" " +
                            $"Dont use 'Here are the filtered items:' in the output. Check this demo output template, you should return output like this: {outputTemplate} ";

                var completion = await _azureAIService.GetCompletion(prompt, cancellationToken);

                var filteredCountryNames = completion.Split('\n').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToList();

                if (FilteredCountries.Count > 0)
                    FilteredCountries.Clear();
                if (completion.ToLower().Trim() != "empty")
                {
                    foreach (var country in filteredCountryNames)
                    {
                        FilteredCountries.Add(new CountryModel { Name = country });
                    }
                }
            }
            return FilteredCountries;
        }

    }
}
