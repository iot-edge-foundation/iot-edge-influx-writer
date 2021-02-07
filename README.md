# iot-edge-influx-writer

Example on how to write telemetry to an InfluxDB using Azure IoT Edge

## Introduction

This Azure IoT Edge module demonstrates how to write routed telemetry to a local InfluxDB database.

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

This library is available under MIT license.
