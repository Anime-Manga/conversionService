## 💱Conversion Service
Questo progetto verrà utilizzato per convertire file ts in mp4 da poter riprodurre in streaming
### Information general:
- `require` volume mounted on Docker
### Variabili globali richiesti:
```sh
example:
    #--- rabbit ---
    USERNAME_RABBIT: "guest" #guest [default]
    PASSWORD_RABBIT: "guest" #guest [default]
    ADDRESS_RABBIT: "localhost" #localhost [default]
    LIMIT_CONSUMER_RABBIT: "5" #3 [default]
    
    #--- API ---
    ADDRESS_API: "localhost" #localhost [default]
    PORT_API: "33333" #3000 [default]
    PROTOCOL_API: "http" or "https" #http [default]
    
    #--- Logger ---
    LOG_LEVEL: "Debug|Info|Error" #Info [default]
    WEBHOOK_DISCORD_DEBUG: "url" [not require]
    
    #--- General ---
    MAX_THREAD: "3" #3 default
    PATH_TEMP: "/folder/temp" [require]
    PATH_FFMPEG: "/folder/bin" #/usr/local/bin/ffmpeg [default]
    BASE_PATH: "/folder/anime" or "D:\\\\Directory\Anime" #/ [default]
```