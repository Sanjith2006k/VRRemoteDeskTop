import re

SCENE_PATH = 'Assets/Scenes/Main.unity'

with open(SCENE_PATH, 'r', encoding='utf-8') as f:
    content = f.read()

# Blocks to update: map block ID to field replacements
updates = {
    # Desk_Top
    '924274517': {
        'm_LocalScale': '{x: 2.1, y: 0.05, z: 0.85}',
        'm_LocalPosition': '{x: 0, y: 0.75, z: 0}',
    },
    # Desk_Leg_L
    '816064732': {
        'm_LocalPosition': '{x: -0.98, y: 0.365, z: 0}',
        'm_LocalScale': '{x: 0.06, y: 0.73, z: 0.75}',
    },
    # Desk_Leg_R
    '1092953156': {
        'm_LocalPosition': '{x: 0.98, y: 0.365, z: 0}',
        'm_LocalScale': '{x: 0.06, y: 0.73, z: 0.75}',
    },
    # Desk_Crossbar
    '1493751236': {
        'm_LocalPosition': '{x: 0, y: 0.35, z: 0.28}',
        'm_LocalScale': '{x: 1.96, y: 0.06, z: 0.12}',
    },
    # Desk_Accent_Cyan
    '1981691231': {
        'm_LocalPosition': '{x: 0, y: 0.725, z: -0.41}',
        'm_LocalScale': '{x: 2.02, y: 0.012, z: 0.03}',
    },
    # Desk_Accent_Violet
    '1087780041': {
        'm_LocalPosition': '{x: 0, y: 0.725, z: 0.41}',
        'm_LocalScale': '{x: 2.02, y: 0.012, z: 0.03}',
    },
    # Desk_Mat
    '1842265967': {
        'm_LocalPosition': '{x: 0, y: 0.7775, z: -0.14}',
        'm_LocalScale': '{x: 0.95, y: 0.005, z: 0.4}',
    },
    # Keyboard
    '989966332': {
        'm_LocalPosition': '{x: -0.06, y: 0.782, z: -0.15}',
        'm_LocalRotation': '{x: 0.034899497, y: 0, z: 0, w: 0.99939084}',
        'm_LocalEulerAnglesHint': '{x: 4, y: 0, z: 0}',
    },
    # Mouse
    '1523159169': {
        'm_LocalPosition': '{x: 0.3, y: 0.785, z: -0.15}',
        'm_LocalRotation': '{x: 0, y: 0, z: 0, w: 1}',
        'm_LocalEulerAnglesHint': '{x: 0, y: 0, z: 0}',
    },
    # Speakers root
    '1723680476': {
        'm_LocalPosition': '{x: 0, y: 0, z: 0}',
        'm_LocalRotation': '{x: 0, y: 0, z: 0, w: 1}',
        'm_LocalScale': '{x: 1, y: 1, z: 1}',
        'm_LocalEulerAnglesHint': '{x: 0, y: 0, z: 0}',
    },
    # Speaker_L
    '65122605': {
        'm_LocalPosition': '{x: -0.74, y: 0.915, z: 0.12}',
        'm_LocalRotation': '{x: 0, y: 0.1305262, z: 0, w: 0.9914449}',
        'm_LocalEulerAnglesHint': '{x: 0, y: 15, z: 0}',
    },
    # Speaker_R
    '1950806519': {
        'm_LocalPosition': '{x: 0.74, y: 0.915, z: 0.12}',
        'm_LocalRotation': '{x: 0, y: -0.1305262, z: 0, w: 0.9914449}',
        'm_LocalEulerAnglesHint': '{x: 0, y: -15, z: 0}',
    },
    # PC_Tower
    '752389437': {
        'm_LocalPosition': '{x: 0.94, y: 1.015, z: 0.1}',
        'm_LocalRotation': '{x: 0, y: 0, z: 0, w: 1}',
        'm_LocalEulerAnglesHint': '{x: 0, y: 0, z: 0}',
    },
    # StatusDisplay
    '1837484608': {
        'm_LocalPosition': '{x: -0.52, y: 0.835, z: 0.14}',
        'm_LocalRotation': '{x: -0.12782298, y: 0.17066927, z: 0.022370726, w: 0.97673553}',
        'm_LocalEulerAnglesHint': '{x: -15, y: 20, z: 0}',
    },
    # MouseLook script on Camera
    '1731231715': {
        '_enableSmoothing': '1',
        '_smoothSpeed': '25',
    }
}

docs = content.split('--- !u!')
new_docs = [docs[0]]

updated_count = 0
for d in docs[1:]:
    lines = d.split('\n')
    m = re.match(r'(\d+)\s+&(\d+)', lines[0].strip())
    if m and m.group(2) in updates:
        block_id = m.group(2)
        field_dict = updates[block_id]
        new_lines = []
        for l in lines:
            replaced = False
            for k, v in field_dict.items():
                if re.match(r'^\s*' + re.escape(k) + r':', l):
                    indent = l[:len(l) - len(l.lstrip())]
                    new_lines.append(f'{indent}{k}: {v}')
                    replaced = True
                    break
            if not replaced:
                new_lines.append(l)
        new_docs.append('--- !u!' + '\n'.join(new_lines))
        updated_count += 1
    else:
        new_docs.append('--- !u!' + d)

print(f'Updated {updated_count} blocks in Main.unity')

final_content = ''.join(new_docs)

with open(SCENE_PATH, 'w', encoding='utf-8') as f:
    f.write(final_content)

print('Main.unity successfully saved!')
