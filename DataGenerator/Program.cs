using Bogus;
using Bogus.Extensions;
using Events.EFModel.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text;

const int participantsPerCountry = 50;
Console.OutputEncoding = Encoding.UTF8;
const string defaultOutputPath = @"..\..\..\..\docker-definitions\postgres-eventsdb\init\06-people.sql";

var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("EventDB")
    ?? throw new InvalidOperationException("Connection string 'EventDB' was not found.");

builder.Services.AddDbContext<EventsContext>(options => options.UseNpgsql(connectionString));

using var host = builder.Build();
using var scope = host.Services.CreateScope();

var dbContext = scope.ServiceProvider.GetRequiredService<EventsContext>();

var outputPath = PromptForOutputPath(defaultOutputPath);
var countryNames = await LoadCountryNamesAsync(dbContext);
var insertStatements = BuildPersonInsertStatements(countryNames);
var fullOutputPath = Path.GetFullPath(outputPath, Environment.CurrentDirectory);

Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath)!);
await File.WriteAllTextAsync(fullOutputPath, string.Join("\n", insertStatements) + "\n", new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

Console.WriteLine($"SQL script saved to: {fullOutputPath}");

static async Task<Dictionary<string, string>> LoadCountryNamesAsync(EventsContext dbContext)
{
    return await dbContext.Countries
        .AsNoTracking()
        .ToDictionaryAsync(country => country.Code, country => country.Name);
}

static string PromptForOutputPath(string defaultPath)
{
    Console.Write($"Enter SQL output [{defaultPath}]: ");
    var input = Console.ReadLine();

    return string.IsNullOrWhiteSpace(input) ? defaultPath : input.Trim();
}

static List<string> BuildPersonInsertStatements(IReadOnlyDictionary<string, string> countryNames)
{    
    var statements = new List<string>();
    var nonLatinLocales = new HashSet<string>
    {
        "ar",
        "el",
        "fa",
        "ge",
        "ne",
        "ru",
        "uk"
    };

    var localeToCountryCode = new (string Locale, string CountryCode)[]
    {
        ("af_ZA", "ZA"),
        ("ar", "SA"),
        ("az", "AZ"),
        ("cz", "CZ"),
        ("de", "DE"),
        ("de_AT", "AT"),
        ("de_CH", "CH"),
        ("el", "GR"),
        ("en_AU", "AU"),
        ("en_CA", "CA"),
        ("en_GB", "GB"),
        ("en_IE", "IE"),
        ("en_IND", "IN"),
        ("en_NG", "NG"),
        ("en_US", "US"),
        ("es", "ES"),
        ("es_MX", "MX"),
        ("fa", "IR"),
        ("fi", "FI"),
        ("fr", "FR"),
        ("ge", "GE"),
        ("hr", "HR"),
        ("id_ID", "ID"),
        ("it", "IT"),
        ("lv", "LV"),
        ("nb_NO", "NO"),
        ("ne", "NP"),
        ("nl", "NL"),
        ("nl_BE", "BE"),
        ("pl", "PL"),
        ("pt_BR", "BR"),
        ("pt_PT", "PT"),
        ("ro", "RO"),
        ("ru", "RU"),
        ("sk", "SK"),
        ("sv", "SE"),
        ("tr", "TR"),
        ("uk", "UA"),
        ("vi", "VN")
    };

    foreach (var (locale, countryCode) in localeToCountryCode)
    {
        var faker = new Faker(locale);
        var transliterationLanguage = locale.Split('_')[0];
        var useTranscriptionForAddress = nonLatinLocales.Contains(transliterationLanguage);
        var addressCountry = NormalizeForStorage(countryNames.GetValueOrDefault(countryCode, countryCode), transliterationLanguage, useTranscriptionForAddress);

        for (var i = 0; i < participantsPerCountry; i++)
        {
            var firstName = faker.Name.FirstName();
            var lastName = faker.Name.LastName();
            var firstNameTranscription = firstName.Transliterate(transliterationLanguage);
            var lastNameTranscription = lastName.Transliterate(transliterationLanguage);
            var city = NormalizeForStorage(faker.Address.City(), transliterationLanguage, useTranscriptionForAddress);
            var addressLine = NormalizeForStorage(faker.Address.StreetAddress(), transliterationLanguage, useTranscriptionForAddress);
            var postalCode = NormalizeForStorage(faker.Address.ZipCode(), transliterationLanguage, useTranscriptionForAddress);
            var email = faker.Internet.Email(firstNameTranscription, lastNameTranscription);
            var contactPhone = NormalizePhoneNumber(faker.Phone.PhoneNumber());
            var birthDate = faker.Date.BetweenDateOnly(new DateOnly(1950, 1, 1), new DateOnly(2010, 12, 31));
            var documentNumber = $"{countryCode}-{faker.Random.Replace("########")}";

            statements.Add(
                "INSERT INTO person (first_name, last_name, first_name_transcription, last_name_transcription, address_line, postal_code, city, address_country, email, contact_phone, birth_date, document_number, country_code) " +
                $"VALUES ('{EscapeSql(firstName)}', '{EscapeSql(lastName)}', '{EscapeSql(firstNameTranscription)}', '{EscapeSql(lastNameTranscription)}', '{EscapeSql(addressLine)}', '{EscapeSql(postalCode)}', '{EscapeSql(city)}', '{EscapeSql(addressCountry)}', '{EscapeSql(email)}', '{EscapeSql(contactPhone)}', '{birthDate:yyyy-MM-dd}', '{EscapeSql(documentNumber)}', '{countryCode}');");
        }
    }

    return statements;
}

static string EscapeSql(string value) => value.Replace("'", "''");

static string NormalizeForStorage(string value, string transliterationLanguage, bool useTranscriptionForAddress) =>
    useTranscriptionForAddress ? value.Transliterate(transliterationLanguage) : value;

static string NormalizePhoneNumber(string value) =>
    value.Replace("\r", string.Empty).Replace("\n", " ").Trim();
