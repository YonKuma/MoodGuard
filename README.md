# Mood Guard
A Stardew Valley mod that prevents animals from losing happiness just because they're inside after 6pm.

# Config

The mod config can be edited by changing the file `config.json`. By default, the file looks like this:
```json
{
    "NightFix": {
        "Enabled": true,
        "Mode": "Standard"
    }
}
```

Changing `Enabled` to `false` will disable the fix for post-6pm happiness drain.

There are three different modes that you can choose from:
* `Standard` - Animals will no longer lose happiness from you being awake after 6pm
* `Increased` - Animals will gain happiness by being inside after 6pm
* `Maximized` - Animals' happiness will continually be set to maximum (255)

# Download
Downloads can currently be found at the [project releases](https://github.com/YonKuma/NightChicken/releases).
