import pycountry
import json
from babel import Locale


def generate_minimal_sql():
    print("Generating SQL: Code, Alpha3, Name + JSON (hr, mk)...")

    try:
        # Initialize only the required languages
        lang_hr = Locale('hr')
        lang_mk = Locale('mk')
    except Exception as e:
        print(f"Babel error: {e}. Check whether you installed the 'Babel' package.")
        return

    # SQL header aligned with your structure (without local_name)
    sql_header = "INSERT INTO country (code, alpha3, name, translations) VALUES\n"
    rows = []

    # Fetch and sort all ISO 3166 countries
    countries = sorted(list(pycountry.countries), key=lambda x: x.alpha_2)

    for country in countries:
        code = country.alpha_2
        alpha3 = country.alpha_3

        # Base English name from the ISO standard
        english_name = getattr(country, 'common_name', country.name)

        # Fetch translations through Babel
        croatian_name = lang_hr.territories.get(code, english_name)
        macedonian_name = lang_mk.territories.get(code, english_name)

        # SQL escaping (if the name contains an apostrophe, e.g. Cote d'Ivoire)
        s_en = english_name.replace("'", "''")
        s_hr = croatian_name.replace("'", "''")
        s_mk = macedonian_name.replace("'", "''")

        # JSON object with only two languages
        trans_dict = {
            "hr": s_hr,
            "mk": s_mk
        }

        # ensure_ascii=False keeps Macedonian Cyrillic readable
        trans_json = json.dumps(trans_dict, ensure_ascii=False).replace("'", "''")

        # Format the row for your INSERT
        row = f"('{code}', '{alpha3}', '{s_en}', '{trans_json}')"
        rows.append(row)

    # Combine and write to file
    final_sql = sql_header + ",\n".join(rows) + ";"

    with open("countries_hr_mk.sql", "w", encoding="utf-8") as f:
        f.write(final_sql)

    print(f"Success! Generated {len(rows)} countries in 'countries_hr_mk.sql'.")


if __name__ == "__main__":
    generate_minimal_sql()
