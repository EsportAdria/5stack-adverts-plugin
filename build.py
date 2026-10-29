import json, sys, datetime
up = json.load(open("upstream.json"))
entry = json.load(open(sys.argv[1]))
entry.pop("$schema", None)
v = entry["variants"]["swiftlys2"]
entry["versions"] = [{
    "version": "2.0.1", "runtime": "swiftlys2",
    "url": "https://github.com/EsportAdria/5stack-adverts-plugin/releases/download/v2.0.1/EA_Adverts-v2.0.1.zip",
    "sha256": "904c442d24141f46ec4fa3516d7cf6b40e46381e6e5c2d234f3fcd84d71f11ce",
    "size": 25274, "published_at": "2026-10-29T16:13:24Z", "prerelease": False,
    "layout": v["layout"], "install_path": v["install_path"],
}, {
    "version": "2.0.0", "runtime": "swiftlys2",
    "url": "https://github.com/EsportAdria/5stack-adverts-plugin/releases/download/v2.0.0/EA_Adverts-v2.0.0.zip",
    "sha256": "e51a465270f081e0f2b0d26651cb3072a74df4ad4c0f455c4575fe0cd72ab696",
    "size": 24015, "published_at": "2026-10-29T15:01:02Z", "prerelease": False,
    "layout": v["layout"], "install_path": v["install_path"],
}]
plugins = [p for p in up["plugins"] if p["slug"] != entry["slug"]] + [entry]
plugins.sort(key=lambda p: p["slug"])
out = dict(up, generated_at=datetime.datetime.now(datetime.timezone.utc).isoformat().replace("+00:00", "Z"), plugins=plugins)
json.dump(out, open("index.json", "w"), indent=2)
print(len(up["plugins"]), "->", len(plugins), [p["slug"] for p in plugins])
