&larr; [README](../README.md)

**SMAPI.Web** contains the code for the `smapi.io` website, including the mod compatibility list
and update check API.

## Contents
* [Log parser](#log-parser)
* [JSON validator](#json-validator)
* [Web API](#web-api)
* [For SMAPI developers](#for-smapi-developers)
  * [Local development](#local-development)
  * [Deploying to Amazon Beanstalk](#deploying-to-amazon-beanstalk)

## Log parser
The log parser provides a web UI for uploading, parsing, and sharing SMAPI logs. The logs are
persisted in a compressed form to Pastebin. The log parser lives at https://log.smapi.io.

## JSON validator
### Overview
The JSON validator provides a web UI for uploading and sharing JSON files, and validating them as
plain JSON or against a predefined format like `manifest.json` or Content Patcher's `content.json`.
The JSON validator lives at https://json.smapi.io.

### Schema file format
Schema files are defined in `wwwroot/schemas` using the [JSON Schema](https://json-schema.org/)
format. The JSON validator UI recognises a superset of the standard fields to change output:

<dl>
<dt>Documentation URL</dt>
<dd>

The root schema may have a `@documentationURL` field, which is a web URL for the user
documentation:
```js
"@documentationUrl": "https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Manifest"
```

If present, this is shown in the JSON validator UI.

</dd>
<dt>Error messages</dt>
<dd>

Any part of the schema can define an `@errorMessages` field, which overrides matching schema
errors. You can override by error code (recommended), or by error type and a regex pattern matched
against the error message (more fragile):

```js
// by error type
"pattern": "^[a-zA-Z0-9_.-]+\\.dll$",
"@errorMessages": {
   "pattern": "Invalid value; must be a filename ending with .dll."
}
```
```js
// by error type + message pattern
"@errorMessages": {
   "oneOf:valid against no schemas": "Missing required field: EntryDll or ContentPackFor.",
   "oneOf:valid against more than one schema": "Can't specify both EntryDll or ContentPackFor, they're mutually exclusive."
}
```

Error messages may contain special tokens:

* The `@value` token is replaced with the error's value field. This is usually (but not always) the
  original field value.
* When an error has child errors, by default they're flattened into one message:
  ```
  line | field      | error
  ---- | ---------- | -------------------------------------------------------------------------
  4    | Changes[0] | JSON does not match schema from 'then'.
       |            |   ==> Changes[0].ToArea.Y: Invalid type. Expected Integer but got String.
       |            |   ==> Changes[0].ToArea: Missing required fields: Height.
  ```

  If you set the message for an error to `$transparent`, the parent error is omitted entirely and
  the child errors are shown instead:
  ```
  line | field               | error
  ---- | ------------------- | ----------------------------------------------
  8    | Changes[0].ToArea.Y | Invalid type. Expected Integer but got String.
  8    | Changes[0].ToArea   | Missing required fields: Height.
  ```

  The child errors themselves may be marked `$transparent`, etc. If an error has no child errors,
  this override is ignored.

  Validation errors for `then` blocks are transparent by default, unless overridden.

</dd>
</dl>

### Using a schema file directly
You can reference the validator schemas in your JSON file directly using the `$schema` field, for
text editors that support schema validation. For example:
```js
{
   "$schema": "https://smapi.io/schemas/manifest.json",
   "Name": "Some mod",
   ...
}
```

Current schemas:

format | schema URL
------ | ----------
[SMAPI `manifest.json`](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Manifest) | https://smapi.io/schemas/manifest.json
[Content Patcher `content.json`](https://github.com/Pathoschild/StardewMods/tree/develop/ContentPatcher#readme) | https://smapi.io/schemas/content-patcher.json

## Web API
### Overview
SMAPI provides a web API at `api.smapi.io` for use by SMAPI and external tools. The URL includes a
`{version}` token, which is the SMAPI version for backwards compatibility. This API is publicly
accessible but not officially released; it may change at any time.

### `/mods` endpoint
The API has one `/mods` endpoint. This provides mod info, including official versions and URLs
(from Chucklefish, GitHub, or Nexus), unofficial versions from the wiki, and optional mod metadata
from the wiki and SMAPI's internal data. This is used by SMAPI to perform update checks, and by
external tools to fetch mod data.

The API accepts a `POST` request with the mods to match, each of which **must** specify an ID and
may _optionally_ specify [update keys](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Manifest#Update_checks).
The API will automatically try to fetch known update keys from the wiki and internal data based on
the given ID.

```
POST https://api.smapi.io/v2.0/mods
{
   "mods": [
      {
         "id": "Pathoschild.LookupAnything",
         "updateKeys": [ "nexus:541", "chucklefish:4250" ]
      }
   ],
   "includeExtendedMetadata": true
}
```

The API will automatically aggregate versions and errors. Each mod will include...
* an `id` (matching what you passed in);
* up to three versions: `main` (e.g. 'latest version' field on Nexus), `optional` if newer (e.g.
  optional files on Nexus), and `unofficial` if newer (from the wiki);
* `metadata` with mod info crossreferenced from the wiki and internal data (only if you specified
  `includeExtendedMetadata: true`);
* and `errors` containing any error messages that occurred while fetching data.

For example:
```
[
   {
      "id": "Pathoschild.LookupAnything",
      "main": {
         "version": "1.19",
         "url": "https://www.nexusmods.com/stardewvalley/mods/541"
      },
      "metadata": {
         "id": [
            "Pathoschild.LookupAnything",
            "LookupAnything"
         ],
         "name": "Lookup Anything",
         "nexusID": 541,
         "gitHubRepo": "Pathoschild/StardewMods",
         "compatibilityStatus": "Ok",
         "compatibilitySummary": "✓ use latest version."
      },
      "errors": []
   }
]
```

## For SMAPI developers
### Local environment
A local environment lets you run a complete copy of the web project (including cache database) on
your machine, with no external dependencies aside from the actual mod sites.

Initial setup:

1. [Install MongoDB](https://docs.mongodb.com/manual/administration/install-community/) and add its
   `bin` folder to the system PATH.
2. Create a local folder for the MongoDB data (e.g. `C:\dev\smapi-cache`).
3. Enter your credentials in the `appsettings.Development.json` file. You can leave the MongoDB
   credentials as-is to use the default local instance; see the next section for the other settings.

To launch the environment:
1. Launch MongoDB from a terminal (change the data path if applicable):
    ```sh
    mongod --dbpath C:\dev\smapi-cache
    ```
2. Launch `SMAPI.Web` from Visual Studio to run a local version of the site.  
    <small>(Local URLs will use HTTP instead of HTTPS, and subdomains will become routes, like
    `log.smapi.io` &rarr; `localhost:59482/log`.)</small>

### Production environment
A production environment includes the web servers and cache database hosted online for public
access. This section assumes you're creating a new production environment from scratch (not using
the official live environment).

Initial setup:

1. Launch an empty MongoDB server (e.g. using [MongoDB Atlas](https://www.mongodb.com/cloud/atlas)).
2. Create an AWS Beanstalk .NET environment with these environment properties:

   property name                   | description
   ------------------------------- | -----------------
   `LogParser:PastebinDevKey`      | The [Pastebin developer key](https://pastebin.com/api#1) used to authenticate with the Pastebin API.
   `LogParser:PastebinUserKey`     | The [Pastebin user key](https://pastebin.com/api#8) used to authenticate with the Pastebin API. Can be left blank to post anonymously.
   `LogParser:SectionUrl`          | The root URL of the log page, like `https://log.smapi.io/`.
   `ModUpdateCheck:GitHubPassword` | The password with which to authenticate to GitHub when fetching release info.
   `ModUpdateCheck:GitHubUsername` | The username with which to authenticate to GitHub when fetching release info.
   `MongoDB:Host`                  | The hostname for the MongoDB instance.
   `MongoDB:Username`              | The login username for the MongoDB instance.
   `MongoDB:Password`              | The login password for the MongoDB instance.
   `MongoDB:Database`              | The database name (e.g. `smapi` in production or `smapi-edge` in testing environments).

To deploy updates:
1. Deploy the web project using [AWS Toolkit for Visual Studio](https://aws.amazon.com/visualstudio/).
2. If the MongoDB schema changed, delete the MongoDB database. (It'll be recreated automatically.)
