# RSS_WebScraper
A small tool for fixing my social media addiction.

Follow only people you want without being tracked by algorithms or third parties

Supports:\
Picuki (Instagram)\
Nitter (Twitter/X)\
ProxiTok (Tiktok)

Installation:
1. Get the release from [here](https://github.com/dlabaja/RSS_WebScraper/releases/tag/1.0)
2. Extract it into any folder
3. Set values in data/config.json:
- url - url of the local rss server
- ffmpeg_location - location of ffmpeg (download [here](https://ffmpeg.org/))
- curl_impersonate_script_location - special version of curl, path to one of the browser scripts (eg. ff109) (download [here](https://github.com/lwthiker/curl-impersonate))
- scrape_timer - how many minutes it take to rescrape
- nitter_instance - [listed here](https://github.com/zedeus/nitter/wiki/Instances), default nitter.net
- proxitok_instance - [listed here](https://github.com/pablouser1/ProxiTok/wiki/Public-instances), default proxitok.pabloferreiro.es
- sites_and_usernames - put all accounts you want to scrape to the list. Picuki_stories_blacklist ignores stories and nitter_replies_blacklist ignores user replies
4. Run the start.sh script (ideally on system boot)
5. After scraping, all rss urls are located in data/rss_urls.txt
