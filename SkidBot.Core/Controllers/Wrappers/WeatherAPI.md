https://www.weatherapi.com/docs/

https://www.weatherapi.com/api-explorer.aspx

![screenshot](https://res.kate.pet/upload/5d1fbff5-1b92-4b4d-8de4-193b1227dc41/DiscordCanary_1MAWlBHUSi.png)

![screenshot](https://res.kate.pet/upload/a877dabf-7093-485e-aec7-803b4f47d052/DiscordCanary_ujCqnesytK.png)

![screenshot](https://res.kate.pet/upload/e051dd61-7d7a-4649-9036-8346067ba6da/DiscordCanary_c9ByQokJte.png)

![screenshot](https://res.kate.pet/upload/32d117ac-352d-4520-9184-9e49afa99be9/DiscordCanary_7JPpBbzsOy.png)

![screenshot](https://res.kate.pet/upload/6a0d6cf1-a41e-47c2-9908-521e0681bb70/DiscordCanary_g8jyprOINf.png)

![screenshot](https://res.kate.pet/upload/5b680bd7-6470-415f-adec-6ff683f24648/DiscordCanary_I95RkSneCb.png)

![screenshot](https://res.kate.pet/upload/e13542dd-0719-405d-ae57-a9c5c61b19ca/DiscordCanary_5XS2Tv1pHO.png)

![screenshot](https://res.kate.pet/upload/ff7b8b89-af59-455a-91e8-1e0cf32fc106/DiscordCanary_wvQvd7NfaM.png)


# Current Weather
Call
```bash
curl http://api.weatherapi.com/v1/current.json?key=cc5d8680cb524995bec122310221812&q=Perth%2C%20Western%20Australia&aqi=no
```

Response
```json
{
    "location": {
        "name": "Perth",
        "region": "Western Australia",
        "country": "Australia",
        "lat": -31.93,
        "lon": 115.83,
        "tz_id": "Australia/Perth",
        "localtime_epoch": 1676973044,
        "localtime": "2023-02-21 17:50"
    },
    "current": {
        "last_updated_epoch": 1676972700,
        "last_updated": "2023-02-21 17:45",
        "temp_c": 26.0,
        "temp_f": 78.8,
        "is_day": 1,
        "condition": {
            "text": "Sunny",
            "icon": "//cdn.weatherapi.com/weather/64x64/day/113.png",
            "code": 1000
        },
        "wind_mph": 16.1,
        "wind_kph": 25.9,
        "wind_degree": 210,
        "wind_dir": "SSW",
        "pressure_mb": 1009.0,
        "pressure_in": 29.8,
        "precip_mm": 0.0,
        "precip_in": 0.0,
        "humidity": 61,
        "cloud": 0,
        "feelslike_c": 27.5,
        "feelslike_f": 81.5,
        "vis_km": 10.0,
        "vis_miles": 6.0,
        "uv": 7.0,
        "gust_mph": 18.1,
        "gust_kph": 29.2
    }
}
```

## With Air Quality
```bash
curl http://api.weatherapi.com/v1/current.json?key=cc5d8680cb524995bec122310221812&q=Perth%2C%20Western%20Australia&aqi=yes
```

Response
```json
{
    "location": {
        "name": "Perth",
        "region": "Western Australia",
        "country": "Australia",
        "lat": -31.93,
        "lon": 115.83,
        "tz_id": "Australia/Perth",
        "localtime_epoch": 1676973214,
        "localtime": "2023-02-21 17:53"
    },
    "current": {
        "last_updated_epoch": 1676972700,
        "last_updated": "2023-02-21 17:45",
        "temp_c": 26.0,
        "temp_f": 78.8,
        "is_day": 1,
        "condition": {
            "text": "Sunny",
            "icon": "//cdn.weatherapi.com/weather/64x64/day/113.png",
            "code": 1000
        },
        "wind_mph": 16.1,
        "wind_kph": 25.9,
        "wind_degree": 210,
        "wind_dir": "SSW",
        "pressure_mb": 1009.0,
        "pressure_in": 29.8,
        "precip_mm": 0.0,
        "precip_in": 0.0,
        "humidity": 61,
        "cloud": 0,
        "feelslike_c": 27.5,
        "feelslike_f": 81.5,
        "vis_km": 10.0,
        "vis_miles": 6.0,
        "uv": 7.0,
        "gust_mph": 18.1,
        "gust_kph": 29.2,
        "air_quality": {
            "co": 223.60000610351562,
            "no2": 4.300000190734863,
            "o3": 57.20000076293945,
            "so2": 4.099999904632568,
            "pm2_5": 4.0,
            "pm10": 10.300000190734863,
            "us-epa-index": 1,
            "gb-defra-index": 1
        }
    }
}
```

# Forecast
```
curl "http://api.weatherapi.com/v1/forecast.json?key=cc5d8680cb524995bec122310221812&q=Perth%2C%20Western%20Australia&days=2&aqi=no&alerts=no"
```

Response
```json

```