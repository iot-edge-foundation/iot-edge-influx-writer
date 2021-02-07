# svelde-iot-edge-influx-writer

Example on how to write telemetry to an InfluxDB using Azure IoT Edge

## Introduction

This Aure IoT Edge module demonstrates hoe to write telemetry to a local InfluxDB database.

Keep in mind you have to create the database yourself.

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

This library is available under MIT license
