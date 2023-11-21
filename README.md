# RSS_WebScraper
A small tool for fixing my social media addiction.

Follow only people you want without being tracked by third parties or distracted by addicting algorithms

Supports:\
Picuki (Instagram)\
Nitter (Twitter/X)\
ProxiTok (Tiktok)\
Invidious (YouTube)

Installation:
1. Get the release from [here](https://github.com/dlabaja/RSS_WebScraper/releases/latest)
2. Extract it into any folder
3. Put config.json next to the binary (template [here](https://gist.github.com/dlabaja/79db7b135132c5707167a45afa2ab3ab)):
- url - url of the local rss server
- ffmpeg_location - location of ffmpeg (download [here](https://ffmpeg.org/))
- curl_impersonate_script_location - special version of curl, path to one of the browser scripts (eg. ff109) (download [here](https://github.com/lwthiker/curl-impersonate))
- scrape_timer - how many minutes it take to rescrape
- nitter_instance - [listed here](https://github.com/zedeus/nitter/wiki/Instances), default nitter.net
- proxitok_instance - [listed here](https://github.com/pablouser1/ProxiTok/wiki/Public-instances), default proxitok.pabloferreiro.es
- invidious_instance - [listed here](https://docs.invidious.io/instances), default invidious.poast.org
  - scrapes all accounts you subscribed to on the specific instance
  - to make it work, get your SID cookie value (Press F12 on the instance page -> Storage -> Cookies) and copy it into cookies/invidious.txt in format SID=value_of_the_cookie.
- sites_and_usernames - put all accounts you want to scrape to the list. Picuki_stories_blacklist ignores stories and nitter_replies_blacklist ignores user replies
4. Run the start.sh script (ideally on system boot)
5. After scraping, all rss urls are located in rss_urls.txt
