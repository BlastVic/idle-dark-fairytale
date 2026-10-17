#!/usr/bin/env python3
"""Read-only project-owned Assets structure and migration checks."""
import json
import re
from pathlib import Path

ROOT = next(p for p in Path(__file__).resolve().parents if (p / 'ProjectSettings').is_dir())
ASSETS = ROOT / 'Assets'
VENDOR_ROOTS = ('3rdParty', 'AddressableAssetsData', 'ExternalDependencyManager', 'Plugins', 'Spine', 'Standard Assets', 'TextMesh Pro', 'StreamingAssets')
FIXED_FILES = {
    'Assets/Editor/SpineSettings.asset': 'SpinePreferences.SPINE_SETTINGS_ASSET_PATH',
    'Assets/Resources/IAPProductCatalog.json': 'Unity IAP ProductCatalog.kCatalogPath',
    'Assets/Resources/BillingMode.json': 'Unity IAP UnityPurchasingEditor.ModePath',
}

def inspect():
    issues = []
    for folder in [ASSETS, *sorted(p for p in ASSETS.rglob('*') if p.is_dir())]:
        rel = folder.relative_to(ASSETS)
        if rel.parts and rel.parts[0] in VENDOR_ROOTS:
            continue
        children = [p for p in folder.iterdir() if not p.name.startswith('.') and p.name != '__pycache__']
        if not any(p.is_dir() for p in children):
            continue
        for p in children:
            path = p.relative_to(ROOT).as_posix()
            if p.is_file() and p.suffix != '.meta' and path not in FIXED_FILES:
                issues.append('Mixed category directory: ' + path)
    manifest = json.loads((ASSETS / 'Editor/ResourceAudit/Data/ProjectStructureMigration.json').read_text())
    for entry in manifest['entries']:
        path = ROOT / entry['path']
        if (ROOT / entry['old']).exists():
            issues.append('Old asset path still exists: ' + entry['old'])
        if not path.is_file():
            issues.append('Missing moved asset: ' + entry['path'])
            continue
        meta = Path(str(path) + '.meta')
        match = re.search(r'^guid: (\w+)', meta.read_text(), re.M) if meta.exists() else None
        if not match or match[1] != entry['guid']:
            issues.append('GUID changed: ' + entry['path'])
    guids = {}
    for meta in ASSETS.rglob('*.meta'):
        match = re.search(r'^guid: (\w+)', meta.read_text(), re.M)
        if match:
            if match[1] in guids:
                issues.append('Duplicate GUID: ' + str(meta.relative_to(ROOT)))
            guids[match[1]] = meta
    return {'migrated_assets': len(manifest['entries']), 'issues': issues, 'fixed_path_exceptions': FIXED_FILES}

if __name__ == '__main__':
    result = inspect()
    print(json.dumps(result, ensure_ascii=False, indent=2))
    raise SystemExit(bool(result['issues']))
