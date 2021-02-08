# iot-edge-influx-writer

Example on how to write telemetry to an InfluxDB using Azure IoT Edge

## Introduction

This Azure IoT Edge module demonstrates how to write routed telemetry to a local InfluxDB database.

Keep in mind you have to create the database yourself.

## How to use

This module is a demonstration of how to ingest the Ambiant Temperature of the the [Microsoft Simulated Temperature module](https://azuremarketplace.microsoft.com/en-us/marketplace/apps/azure-iot.simulated-temperature-sensor?tab=overview)

Connect to it with an IoT Edge route:

```
FROM /messages/modules/sim/outputs/temperatureOutput INTO BrokeredEndpoint("/modules/writer/inputs/input1")
```

## Desired properties

An example of the desired properties is:

    {
        "influxDbUrl": "http://192.168.1.91:8086",
        "influxDatabase": "iotedger",
        "tableName": "temperature",
        "tags": {
        "1": {
            "key": "area",
            "value": "ambiant"
        },
        "2": {
            "key": "prodline",
            "value": "1"
        }
    }

## License

This library is available under MIT license.
