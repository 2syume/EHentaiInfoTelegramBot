# EHentaiInfoTelegramBot

A telegram bot that retrieves infos and covers from EHentai/NHentai urls.

## Usage

Create a json file called `config.json` with the following content

```json
{
  "secret": "your telegram bot secret",
  "ipb_member_id": "your ehentai cookie",
  "ipb_pass_hash": "your ehentai cookie"
}
```

Then, run the program using the dotnet core runtime.

This project targets .NET 10, so build/run it with the .NET 10 SDK (or newer).

## Special thanks

This bot uses EHentai Chinese translation database from https://github.com/Mapaler/EhTagTranslator
