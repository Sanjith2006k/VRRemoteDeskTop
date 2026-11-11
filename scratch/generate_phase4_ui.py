import os
import re

SCENE_PATH = 'Assets/Scenes/Main.unity'

# Read scene
with open(SCENE_PATH, 'r', encoding='utf-8') as f:
    content = f.read()

docs = content.split('--- !u!')
header = docs[0]

doc_map = {}
doc_order = []
go_names = {}
tf_to_go = {}
go_to_tf = {}
tf_parent = {}
tf_children = {}
go_comps = {}

for d in docs[1:]:
    lines = d.strip().split('\n')
    m = re.match(r'(\d+)\s+&(\d+)', lines[0])
    if m:
        t_id, f_id = m.group(1), m.group(2)
        doc_map[f_id] = (t_id, d)
        doc_order.append(f_id)
        if t_id == '1':
            nm = re.search(r'm_Name:\s*(.*)', d)
            go_names[f_id] = nm.group(1).strip() if nm else ''
            comps = re.findall(r'component:\s*\{fileID:\s*(\d+)\}', d)
            go_comps[f_id] = comps
        elif t_id in ('4', '224'):
            gm = re.search(r'm_GameObject:\s*\{fileID:\s*(\d+)\}', d)
            pm = re.search(r'm_Father:\s*\{fileID:\s*(\d+)\}', d)
            if gm:
                tf_to_go[f_id] = gm.group(1)
                go_to_tf[gm.group(1)] = f_id
            if pm:
                tf_parent[f_id] = pm.group(1)
            ch = re.findall(r'- \{fileID:\s*(\d+)\}', d)
            tf_children[f_id] = ch

def get_subtree_file_ids(tf_id):
    ids = set()
    ids.add(tf_id)
    g_id = tf_to_go.get(tf_id)
    if g_id:
        ids.add(g_id)
        for c in go_comps.get(g_id, []):
            ids.add(c)
    for ch in tf_children.get(tf_id, []):
        ids.update(get_subtree_file_ids(ch))
    return ids

# Collect all children of DesktopCanvas (tf 734257990) to remove old UI
old_canvas_children_ids = set()
for ch in tf_children.get('734257990', []):
    old_canvas_children_ids.update(get_subtree_file_ids(ch))

# Also remove any previous generated 300000xxx IDs if present
for fid in list(doc_map.keys()):
    if fid.startswith('300000'):
        old_canvas_children_ids.add(fid)

# Make sure VRCursor IDs are cleaned so we recreate them cleanly
for cid in ['416825115', '416825116', '416825117', '416825118', '416825119']:
    old_canvas_children_ids.add(cid)

print(f'Old DesktopCanvas elements to remove: {len(old_canvas_children_ids)}')

# ID Generator starting at 300000100
curr_id = 300000100
def next_id():
    global curr_id
    curr_id += 1
    return str(curr_id)

# GUIDs
TMP_GUID = 'f4688fdb7df04437aeb418b961361dc5'
FONT_GUID = '8f586378b4e144a9851e7b34d9b748ee'
IMAGE_GUID = 'fe87c0e1cc204ed48ad3b37840f39efc'
BUTTON_GUID = '4e29b1a8efbd4b44bb3f3716e73f07ff'
HLAYOUT_GUID = '30649d3a9faa99c48a7b1166b86bf2a0'
VLAYOUT_GUID = '59f4a42c6c5184841961e680a96937e0'
DESKTOP_ICON_GUID = '9a41b248a375421db02847610f639c28'
CURSOR_GUID = '77d8b4e7e971e1448a98b11d18b27293'

def make_gameobject(g_id, name, comp_ids):
    comp_lines = '\n'.join([f'  - component: {{fileID: {cid}}}' for cid in comp_ids])
    doc = f'''1 &{g_id}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
{comp_lines}
  m_Layer: 5
  m_Name: {name}
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
'''
    return doc

def make_rect_transform(tf_id, g_id, parent_tf_id, child_tf_ids, anchor_min, anchor_max, pivot, pos, size):
    ch_lines = '\n'.join([f'  - {{fileID: {cid}}}' for cid in child_tf_ids])
    ch_str = f'  m_Children:\n{ch_lines}' if child_tf_ids else '  m_Children: []'
    doc = f'''224 &{tf_id}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {g_id}}}
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
{ch_str}
  m_Father: {{fileID: {parent_tf_id}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
  m_AnchorMin: {{x: {anchor_min[0]}, y: {anchor_min[1]}}}
  m_AnchorMax: {{x: {anchor_max[0]}, y: {anchor_max[1]}}}
  m_AnchoredPosition: {{x: {pos[0]}, y: {pos[1]}}}
  m_SizeDelta: {{x: {size[0]}, y: {size[1]}}}
  m_Pivot: {{x: {pivot[0]}, y: {pivot[1]}}}
'''
    return doc

def make_canvas_renderer(cr_id, g_id):
    doc = f'''222 &{cr_id}
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {g_id}}}
  m_CullTransparentMesh: 1
'''
    return doc

def make_image(img_id, g_id, color, is_filled=False, fill_amount=1.0):
    fill_lines = f'''  m_Type: 3
  m_PreserveAspect: 0
  m_FillCenter: 1
  m_FillMethod: 0
  m_FillAmount: {fill_amount}
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0''' if is_filled else '''  m_Type: 0
  m_PreserveAspect: 0
  m_FillCenter: 1
  m_FillMethod: 4
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0'''

    doc = f'''114 &{img_id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {g_id}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {IMAGE_GUID}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Image
  m_Material: {{fileID: 0}}
  m_Color: {{r: {color[0]}, g: {color[1]}, b: {color[2]}, a: {color[3]}}}
  m_RaycastTarget: 1
  m_RaycastPadding: {{x: 0, y: 0, z: 0, w: 0}}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: {{fileID: 0}}
{fill_lines}
  m_PixelsPerUnitMultiplier: 1
'''
    return doc

def make_tmp(tmp_id, g_id, text, font_size, color, align_h=1, bold=False):
    font_style = 1 if bold else 0
    doc = f'''114 &{tmp_id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {g_id}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {TMP_GUID}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: Unity.TextMeshPro::TMPro.TextMeshProUGUI
  m_Material: {{fileID: 0}}
  m_Color: {{r: 1, g: 1, b: 1, a: 1}}
  m_RaycastTarget: 1
  m_RaycastPadding: {{x: 0, y: 0, z: 0, w: 0}}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_text: '{text}'
  m_isRightToLeft: 0
  m_fontAsset: {{fileID: 11400000, guid: {FONT_GUID}, type: 2}}
  m_sharedMaterial: {{fileID: 0}}
  m_fontSharedMaterials: []
  m_fontMaterial: {{fileID: 0}}
  m_fontMaterials: []
  m_fontColor32:
    serializedVersion: 2
    rgba: 4294967295
  m_fontColor: {{r: {color[0]}, g: {color[1]}, b: {color[2]}, a: {color[3]}}}
  m_enableVertexGradient: 0
  m_colorMode: 3
  m_fontColorGradient:
    topLeft: {{r: 1, g: 1, b: 1, a: 1}}
    topRight: {{r: 1, g: 1, b: 1, a: 1}}
    bottomLeft: {{r: 1, g: 1, b: 1, a: 1}}
    bottomRight: {{r: 1, g: 1, b: 1, a: 1}}
  m_fontColorGradientPreset: {{fileID: 0}}
  m_spriteAsset: {{fileID: 0}}
  m_tintAllSprites: 0
  m_StyleSheet: {{fileID: 0}}
  m_TextStyleHashCode: -1183493901
  m_overrideHtmlColors: 0
  m_faceColor:
    serializedVersion: 2
    rgba: 4294967295
  m_fontSize: {font_size}
  m_fontSizeBase: {font_size}
  m_fontWeight: 400
  m_enableAutoSizing: 0
  m_fontSizeMin: 18
  m_fontSizeMax: 72
  m_fontStyle: {font_style}
  m_HorizontalAlignment: {align_h}
  m_VerticalAlignment: 512
  m_textAlignment: 65535
  m_characterSpacing: 0
  m_wordSpacing: 0
  m_lineSpacing: 0
  m_lineSpacingMax: 0
  m_paragraphSpacing: 0
  m_charWidthMaxAdj: 0
  m_TextWrappingMode: 0
  m_wordWrappingRatios: 0.4
  m_overflowMode: 0
  m_linkedTextComponent: {{fileID: 0}}
  parentLinkedComponent: {{fileID: 0}}
  m_enableKerning: 1
  m_ActiveFontFeatures: 6e72656b
  m_enableExtraPadding: 0
  checkPaddingRequired: 0
  m_isRichText: 1
  m_parseCtrlCharacters: 1
  m_isOrthographic: 1
  m_isCullingEnabled: 0
  m_horizontalMapping: 0
  m_verticalMapping: 0
  m_uvLineOffset: 0
  m_geometrySortingOrder: 0
  m_IsTextObjectScaleStatic: 0
  m_VertexBufferAutoSizeReduction: 0
  m_useMaxVisibleDescender: 1
  m_pageToDisplay: 1
  m_margin: {{x: 0, y: 0, z: 0, w: 0}}
  m_isUsingLegacyAnimationComponent: 0
  m_isVolumetricText: 0
  m_hasFontAssetChanged: 0
  m_baseMaterial: {{fileID: 0}}
  m_maskOffset: {{x: 0, y: 0, z: 0, w: 0}}
'''
    return doc

def make_button(btn_id, g_id, target_graphic_id):
    doc = f'''114 &{btn_id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {g_id}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {BUTTON_GUID}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Button
  m_Navigation:
    m_Mode: 0
    m_WrapAround: 0
    m_SelectOnUp: {{fileID: 0}}
    m_SelectOnDown: {{fileID: 0}}
    m_SelectOnLeft: {{fileID: 0}}
    m_SelectOnRight: {{fileID: 0}}
  m_Transition: 1
  m_Colors:
    m_NormalColor: {{r: 1, g: 1, b: 1, a: 1}}
    m_HighlightedColor: {{r: 0.9, g: 0.95, b: 1, a: 1}}
    m_PressedColor: {{r: 0.7, g: 0.8, b: 0.9, a: 1}}
    m_SelectedColor: {{r: 0.9, g: 0.95, b: 1, a: 1}}
    m_DisabledColor: {{r: 0.5, g: 0.5, b: 0.5, a: 0.5}}
    m_ColorMultiplier: 1
    m_FadeDuration: 0.1
  m_SpriteState:
    m_HighlightedSprite: {{fileID: 0}}
    m_PressedSprite: {{fileID: 0}}
    m_SelectedSprite: {{fileID: 0}}
    m_DisabledSprite: {{fileID: 0}}
  m_AnimationTriggers:
    m_NormalTrigger: Normal
    m_HighlightedTrigger: Highlighted
    m_PressedTrigger: Pressed
    m_SelectedTrigger: Selected
    m_DisabledTrigger: Disabled
  m_Interactable: 1
  m_TargetGraphic: {{fileID: {target_graphic_id}}}
  m_OnClick:
    m_PersistentCalls:
      m_Calls: []
'''
    return doc

def make_h_layout(hl_id, g_id, spacing, align=3):
    doc = f'''114 &{hl_id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {g_id}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {HLAYOUT_GUID}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.HorizontalLayoutGroup
  m_Padding:
    m_Left: 0
    m_Right: 0
    m_Top: 0
    m_Bottom: 0
  m_ChildAlignment: {align}
  m_Spacing: {spacing}
  m_ChildForceExpandWidth: 0
  m_ChildForceExpandHeight: 0
  m_ChildControlWidth: 0
  m_ChildControlHeight: 0
  m_ChildScaleWidth: 0
  m_ChildScaleHeight: 0
  m_ReverseArrangement: 0
'''
    return doc

def make_v_layout(vl_id, g_id, spacing, align=1):
    doc = f'''114 &{vl_id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {g_id}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {VLAYOUT_GUID}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.VerticalLayoutGroup
  m_Padding:
    m_Left: 0
    m_Right: 0
    m_Top: 0
    m_Bottom: 0
  m_ChildAlignment: {align}
  m_Spacing: {spacing}
  m_ChildForceExpandWidth: 0
  m_ChildForceExpandHeight: 0
  m_ChildControlWidth: 0
  m_ChildControlHeight: 0
  m_ChildScaleWidth: 0
  m_ChildScaleHeight: 0
  m_ReverseArrangement: 0
'''
    return doc

def make_desktop_icon(di_id, g_id, app_name, app_desc, bg_img_id, border_img_id, title_tmp_id, sub_tmp_id, highlight_color, active_dot_go_id=None):
    dot_ref = f'{{fileID: {active_dot_go_id}}}' if active_dot_go_id else '{fileID: 0}'
    sub_ref = f'{{fileID: {sub_tmp_id}}}' if sub_tmp_id else '{fileID: 0}'
    doc = f'''114 &{di_id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {g_id}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {DESKTOP_ICON_GUID}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::VRRemoteDesktop.UI.DesktopIcon
  _appName: {app_name}
  _appDescription: {app_desc}
  _iconBackground: {{fileID: {bg_img_id}}}
  _iconBorder: {{fileID: {border_img_id}}}
  _titleText: {{fileID: {title_tmp_id}}}
  _subText: {sub_ref}
  _activeIndicator: {dot_ref}
  _hoverScale: {{x: 1.08, y: 1.08, z: 1}}
  _highlightColor: {{r: {highlight_color[0]}, g: {highlight_color[1]}, b: {highlight_color[2]}, a: 1}}
'''
    return doc

generated_docs = []

# --- 1. Background ---
bg_go_id = next_id()
bg_tf_id = next_id()
bg_cr_id = next_id()
bg_img_id = next_id()

bt_go_id = next_id()
bt_tf_id = next_id()
bt_cr_id = next_id()
bt_img_id = next_id()

bb_go_id = next_id()
bb_tf_id = next_id()
bb_cr_id = next_id()
bb_img_id = next_id()

bg_go_doc = make_gameobject(bg_go_id, 'Background', [bg_tf_id, bg_cr_id, bg_img_id])
bg_tf_doc = make_rect_transform(bg_tf_id, bg_go_id, '734257990', [bt_tf_id, bb_tf_id], (0, 0), (1, 1), (0.5, 0.5), (0, 0), (0, 0))
bg_cr_doc = make_canvas_renderer(bg_cr_id, bg_go_id)
bg_img_doc = make_image(bg_img_id, bg_go_id, (0.035, 0.05, 0.08, 1.0))

bt_go_doc = make_gameobject(bt_go_id, 'BorderTop', [bt_tf_id, bt_cr_id, bt_img_id])
bt_tf_doc = make_rect_transform(bt_tf_id, bt_go_id, bg_tf_id, [], (0, 1), (1, 1), (0.5, 1), (0, 0), (0, 1.5))
bt_cr_doc = make_canvas_renderer(bt_cr_id, bt_go_id)
bt_img_doc = make_image(bt_img_id, bt_go_id, (0.0, 0.9, 1.0, 0.35))

bb_go_doc = make_gameobject(bb_go_id, 'BorderBottom', [bb_tf_id, bb_cr_id, bb_img_id])
bb_tf_doc = make_rect_transform(bb_tf_id, bb_go_id, bg_tf_id, [], (0, 0), (1, 0), (0.5, 0), (0, 0), (0, 1.5))
bb_cr_doc = make_canvas_renderer(bb_cr_id, bb_go_id)
bb_img_doc = make_image(bb_img_id, bb_go_id, (0.6, 0.1, 1.0, 0.35))

generated_docs.extend([
    bg_go_doc, bg_tf_doc, bg_cr_doc, bg_img_doc,
    bt_go_doc, bt_tf_doc, bt_cr_doc, bt_img_doc,
    bb_go_doc, bb_tf_doc, bb_cr_doc, bb_img_doc
])

# --- 2. TopBar ---
topbar_go_id = next_id()
topbar_tf_id = next_id()
topbar_cr_id = next_id()
topbar_img_id = next_id()

tb_trim_go_id = next_id()
tb_trim_tf_id = next_id()
tb_trim_cr_id = next_id()
tb_trim_img_id = next_id()

tb_trim_go_doc = make_gameobject(tb_trim_go_id, 'TopBar_Trim', [tb_trim_tf_id, tb_trim_cr_id, tb_trim_img_id])
tb_trim_tf_doc = make_rect_transform(tb_trim_tf_id, tb_trim_go_id, topbar_tf_id, [], (0, 0), (1, 0), (0.5, 0), (0, 0), (0, 2))
tb_trim_cr_doc = make_canvas_renderer(tb_trim_cr_id, tb_trim_go_id)
tb_trim_img_doc = make_image(tb_trim_img_id, tb_trim_go_id, (0.0, 1.0, 1.0, 0.4))

# LeftSection
ls_go_id = next_id()
ls_tf_id = next_id()
ls_hl_id = next_id()

# StationBadge
sb_go_id = next_id()
sb_tf_id = next_id()
sb_cr_id = next_id()
sb_img_id = next_id()

sbt_go_id = next_id()
sbt_tf_id = next_id()
sbt_cr_id = next_id()
sbt_tmp_id = next_id()

sbt_go_doc = make_gameobject(sbt_go_id, 'StationText', [sbt_tf_id, sbt_cr_id, sbt_tmp_id])
sbt_tf_doc = make_rect_transform(sbt_tf_id, sbt_go_id, sb_tf_id, [], (0, 0), (1, 1), (0.5, 0.5), (0, 0), (0, 0))
sbt_cr_doc = make_canvas_renderer(sbt_cr_id, sbt_go_id)
sbt_tmp_doc = make_tmp(sbt_tmp_id, sbt_go_id, 'STATION-01', 11, (0.0, 1.0, 1.0, 1.0), 2, True)

sb_go_doc = make_gameobject(sb_go_id, 'StationBadge', [sb_tf_id, sb_cr_id, sb_img_id])
sb_tf_doc = make_rect_transform(sb_tf_id, sb_go_id, ls_tf_id, [sbt_tf_id], (0, 0.5), (0, 0.5), (0, 0.5), (0, 0), (96, 26))
sb_cr_doc = make_canvas_renderer(sb_cr_id, sb_go_id)
sb_img_doc = make_image(sb_img_id, sb_go_id, (0.0, 0.7, 0.9, 0.18))

# ComputerNameText
cn_go_id = next_id()
cn_tf_id = next_id()
cn_cr_id = next_id()
cn_tmp_id = next_id()
cn_go_doc = make_gameobject(cn_go_id, 'ComputerNameText', [cn_tf_id, cn_cr_id, cn_tmp_id])
cn_tf_doc = make_rect_transform(cn_tf_id, cn_go_id, ls_tf_id, [], (0, 0.5), (0, 0.5), (0, 0.5), (110, 0), (165, 32))
cn_cr_doc = make_canvas_renderer(cn_cr_id, cn_go_id)
cn_tmp_doc = make_tmp(cn_tmp_id, cn_go_id, 'DESKTOP-VR-HOST', 14, (1, 1, 1, 1), 1, True)

# Divider1
d1_go_id = next_id()
d1_tf_id = next_id()
d1_cr_id = next_id()
d1_img_id = next_id()
d1_go_doc = make_gameobject(d1_go_id, 'Divider_1', [d1_tf_id, d1_cr_id, d1_img_id])
d1_tf_doc = make_rect_transform(d1_tf_id, d1_go_id, ls_tf_id, [], (0, 0.5), (0, 0.5), (0, 0.5), (289, 0), (1, 20))
d1_cr_doc = make_canvas_renderer(d1_cr_id, d1_go_id)
d1_img_doc = make_image(d1_img_id, d1_go_id, (1, 1, 1, 0.2))

# NetworkStatusText
ns_go_id = next_id()
ns_tf_id = next_id()
ns_cr_id = next_id()
ns_tmp_id = next_id()
ns_go_doc = make_gameobject(ns_go_id, 'NetworkStatusText', [ns_tf_id, ns_cr_id, ns_tmp_id])
ns_tf_doc = make_rect_transform(ns_tf_id, ns_go_id, ls_tf_id, [], (0, 0.5), (0, 0.5), (0, 0.5), (304, 0), (130, 32))
ns_cr_doc = make_canvas_renderer(ns_cr_id, ns_go_id)
ns_tmp_doc = make_tmp(ns_tmp_id, ns_go_id, 'LAN • 10 Gbps', 13, (0.35, 0.85, 1.0, 1.0), 1, False)

# ResolutionText
res_go_id = next_id()
res_tf_id = next_id()
res_cr_id = next_id()
res_tmp_id = next_id()
res_go_doc = make_gameobject(res_go_id, 'ResolutionText', [res_tf_id, res_cr_id, res_tmp_id])
res_tf_doc = make_rect_transform(res_tf_id, res_go_id, ls_tf_id, [], (0, 0.5), (0, 0.5), (0, 0.5), (448, 0), (140, 32))
res_cr_doc = make_canvas_renderer(res_cr_id, res_go_id)
res_tmp_doc = make_tmp(res_tmp_id, res_go_id, '1920×1080 @ 60Hz', 12, (0.65, 0.72, 0.85, 1.0), 1, False)

ls_children = [sb_tf_id, cn_tf_id, d1_tf_id, ns_tf_id, res_tf_id]
ls_go_doc = make_gameobject(ls_go_id, 'LeftSection', [ls_tf_id, ls_hl_id])
ls_tf_doc = make_rect_transform(ls_tf_id, ls_go_id, topbar_tf_id, ls_children, (0, 0), (0, 1), (0, 0.5), (24, 0), (620, 0))
ls_hl_doc = make_h_layout(ls_hl_id, ls_go_id, 14, 3)

# CenterSection
cs_go_id = next_id()
cs_tf_id = next_id()

cst_go_id = next_id()
cst_tf_id = next_id()
cst_cr_id = next_id()
cst_tmp_id = next_id()
cst_go_doc = make_gameobject(cst_go_id, 'BrandTitle', [cst_tf_id, cst_cr_id, cst_tmp_id])
cst_tf_doc = make_rect_transform(cst_tf_id, cst_go_id, cs_tf_id, [], (0, 0), (1, 1), (0.5, 0.5), (0, 0), (0, 0))
cst_cr_doc = make_canvas_renderer(cst_cr_id, cst_go_id)
cst_tmp_doc = make_tmp(cst_tmp_id, cst_go_id, 'VR REMOTE DESKTOP OS', 13, (0.78, 0.84, 0.95, 1.0), 2, True)

cs_go_doc = make_gameobject(cs_go_id, 'CenterSection', [cs_tf_id])
cs_tf_doc = make_rect_transform(cs_tf_id, cs_go_id, topbar_tf_id, [cst_tf_id], (0.5, 0), (0.5, 1), (0.5, 0.5), (0, 0), (360, 0))

# RightSection
rs_go_id = next_id()
rs_tf_id = next_id()
rs_hl_id = next_id()

# LatencyText
lat_go_id = next_id()
lat_tf_id = next_id()
lat_cr_id = next_id()
lat_tmp_id = next_id()
lat_go_doc = make_gameobject(lat_go_id, 'LatencyText', [lat_tf_id, lat_cr_id, lat_tmp_id])
lat_tf_doc = make_rect_transform(lat_tf_id, lat_go_id, rs_tf_id, [], (1, 0.5), (1, 0.5), (1, 0.5), (-447, 0), (78, 32))
lat_cr_doc = make_canvas_renderer(lat_cr_id, lat_go_id)
lat_tmp_doc = make_tmp(lat_tmp_id, lat_go_id, '● 22 ms', 13, (0.32, 1.0, 0.56, 1.0), 4, False)

# ConnectionBadge
cb_go_id = next_id()
cb_tf_id = next_id()
cb_cr_id = next_id()
cb_img_id = next_id()

cbt_go_id = next_id()
cbt_tf_id = next_id()
cbt_cr_id = next_id()
cbt_tmp_id = next_id()
cbt_go_doc = make_gameobject(cbt_go_id, 'ConnectionText', [cbt_tf_id, cbt_cr_id, cbt_tmp_id])
cbt_tf_doc = make_rect_transform(cbt_tf_id, cbt_go_id, cb_tf_id, [], (0, 0), (1, 1), (0.5, 0.5), (0, 0), (0, 0))
cbt_cr_doc = make_canvas_renderer(cbt_cr_id, cbt_go_id)
cbt_tmp_doc = make_tmp(cbt_tmp_id, cbt_go_id, 'CONNECTED', 11, (0.32, 1.0, 0.56, 1.0), 2, True)

cb_go_doc = make_gameobject(cb_go_id, 'ConnectionBadge', [cb_tf_id, cb_cr_id, cb_img_id])
cb_tf_doc = make_rect_transform(cb_tf_id, cb_go_id, rs_tf_id, [cbt_tf_id], (1, 0.5), (1, 0.5), (1, 0.5), (-309, 0), (124, 26))
cb_cr_doc = make_canvas_renderer(cb_cr_id, cb_go_id)
cb_img_doc = make_image(cb_img_id, cb_go_id, (0.1, 0.45, 0.2, 0.35))

# Divider2
d2_go_id = next_id()
d2_tf_id = next_id()
d2_cr_id = next_id()
d2_img_id = next_id()
d2_go_doc = make_gameobject(d2_go_id, 'Divider_2', [d2_tf_id, d2_cr_id, d2_img_id])
d2_tf_doc = make_rect_transform(d2_tf_id, d2_go_id, rs_tf_id, [], (1, 0.5), (1, 0.5), (1, 0.5), (-294, 0), (1, 20))
d2_cr_doc = make_canvas_renderer(d2_cr_id, d2_go_id)
d2_img_doc = make_image(d2_img_id, d2_go_id, (1, 1, 1, 0.2))

# TimeText
tim_go_id = next_id()
tim_tf_id = next_id()
tim_cr_id = next_id()
tim_tmp_id = next_id()
tim_go_doc = make_gameobject(tim_go_id, 'TimeText', [tim_tf_id, tim_cr_id, tim_tmp_id])
tim_tf_doc = make_rect_transform(tim_tf_id, tim_go_id, rs_tf_id, [], (1, 0.5), (1, 0.5), (1, 0.5), (-202, 0), (78, 32))
tim_cr_doc = make_canvas_renderer(tim_cr_id, tim_go_id)
tim_tmp_doc = make_tmp(tim_tmp_id, tim_go_id, '12:00:00', 14, (1, 1, 1, 1), 4, True)

# DateText
dat_go_id = next_id()
dat_tf_id = next_id()
dat_cr_id = next_id()
dat_tmp_id = next_id()
dat_go_doc = make_gameobject(dat_go_id, 'DateText', [dat_tf_id, dat_cr_id, dat_tmp_id])
dat_tf_doc = make_rect_transform(dat_tf_id, dat_go_id, rs_tf_id, [], (1, 0.5), (1, 0.5), (1, 0.5), (-100, 0), (88, 32))
dat_cr_doc = make_canvas_renderer(dat_cr_id, dat_go_id)
dat_tmp_doc = make_tmp(dat_tmp_id, dat_go_id, 'FRI, OCT 02', 11, (0.65, 0.72, 0.85, 1.0), 4, False)

# SettingsButton
set_go_id = next_id()
set_tf_id = next_id()
set_cr_id = next_id()
set_img_id = next_id()
set_btn_id = next_id()

sett_go_id = next_id()
sett_tf_id = next_id()
sett_cr_id = next_id()
sett_tmp_id = next_id()
sett_go_doc = make_gameobject(sett_go_id, 'Label', [sett_tf_id, sett_cr_id, sett_tmp_id])
sett_tf_doc = make_rect_transform(sett_tf_id, sett_go_id, set_tf_id, [], (0, 0), (1, 1), (0.5, 0.5), (0, 0), (0, 0))
sett_cr_doc = make_canvas_renderer(sett_cr_id, sett_go_id)
sett_tmp_doc = make_tmp(sett_tmp_id, sett_go_id, 'SETTINGS', 10, (1, 1, 1, 1), 2, True)

set_go_doc = make_gameobject(set_go_id, 'SettingsButton', [set_tf_id, set_cr_id, set_img_id, set_btn_id])
set_tf_doc = make_rect_transform(set_tf_id, set_go_id, rs_tf_id, [sett_tf_id], (1, 0.5), (1, 0.5), (1, 0.5), (0, 0), (86, 28))
set_cr_doc = make_canvas_renderer(set_cr_id, set_go_id)
set_img_doc = make_image(set_img_id, set_go_id, (0.12, 0.16, 0.26, 0.9))
set_btn_doc = make_button(set_btn_id, set_go_id, set_img_id)

rs_children = [lat_tf_id, cb_tf_id, d2_tf_id, tim_tf_id, dat_tf_id, set_tf_id]
rs_go_doc = make_gameobject(rs_go_id, 'RightSection', [rs_tf_id, rs_hl_id])
rs_tf_doc = make_rect_transform(rs_tf_id, rs_go_id, topbar_tf_id, rs_children, (1, 0), (1, 1), (1, 0.5), (-24, 0), (620, 0))
rs_hl_doc = make_h_layout(rs_hl_id, rs_go_id, 14, 5)

topbar_children = [tb_trim_tf_id, ls_tf_id, cs_tf_id, rs_tf_id]
topbar_go_doc = make_gameobject(topbar_go_id, 'TopBar', [topbar_tf_id, topbar_cr_id, topbar_img_id])
topbar_tf_doc = make_rect_transform(topbar_tf_id, topbar_go_id, '734257990', topbar_children, (0, 1), (1, 1), (0.5, 1), (0, 0), (0, 52))
topbar_cr_doc = make_canvas_renderer(topbar_cr_id, topbar_go_id)
topbar_img_doc = make_image(topbar_img_id, topbar_go_id, (0.05, 0.07, 0.12, 0.95))

generated_docs.extend([
    topbar_go_doc, topbar_tf_doc, topbar_cr_doc, topbar_img_doc,
    tb_trim_go_doc, tb_trim_tf_doc, tb_trim_cr_doc, tb_trim_img_doc,
    ls_go_doc, ls_tf_doc, ls_hl_doc,
    sb_go_doc, sb_tf_doc, sb_cr_doc, sb_img_doc,
    sbt_go_doc, sbt_tf_doc, sbt_cr_doc, sbt_tmp_doc,
    cn_go_doc, cn_tf_doc, cn_cr_doc, cn_tmp_doc,
    d1_go_doc, d1_tf_doc, d1_cr_doc, d1_img_doc,
    ns_go_doc, ns_tf_doc, ns_cr_doc, ns_tmp_doc,
    res_go_doc, res_tf_doc, res_cr_doc, res_tmp_doc,
    cs_go_doc, cs_tf_doc,
    cst_go_doc, cst_tf_doc, cst_cr_doc, cst_tmp_doc,
    rs_go_doc, rs_tf_doc, rs_hl_doc,
    lat_go_doc, lat_tf_doc, lat_cr_doc, lat_tmp_doc,
    cb_go_doc, cb_tf_doc, cb_cr_doc, cb_img_doc,
    cbt_go_doc, cbt_tf_doc, cbt_cr_doc, cbt_tmp_doc,
    d2_go_doc, d2_tf_doc, d2_cr_doc, d2_img_doc,
    tim_go_doc, tim_tf_doc, tim_cr_doc, tim_tmp_doc,
    dat_go_doc, dat_tf_doc, dat_cr_doc, dat_tmp_doc,
    set_go_doc, set_tf_doc, set_cr_doc, set_img_doc, set_btn_doc,
    sett_go_doc, sett_tf_doc, sett_cr_doc, sett_tmp_doc
])

# --- 3. Desktop Application Grid (Left Column) ---
grid_go_id = next_id()
grid_tf_id = next_id()
grid_vl_id = next_id()

app_icons_data = [
    ("Terminal", "v4.2 Bash", ">_", (0.0, 1.0, 1.0), (0.04, 0.12, 0.16)),
    ("Files", "1.8 TB SSD", "FILES", (0.6, 0.1, 1.0), (0.12, 0.05, 0.18)),
    ("Browser", "Chromium VR", "WEB", (0.0, 1.0, 1.0), (0.04, 0.12, 0.16)),
    ("SysMonitor", "CPU • GPU • RAM", "MON", (1.0, 0.0, 0.5), (0.16, 0.04, 0.10)),
    ("RemoteLink", "H.265 Ultra", "HOST", (0.2, 0.9, 0.4), (0.04, 0.14, 0.08)),
    ("Keyboard", "On-Screen Keys", "KEYS", (1.0, 0.6, 0.1), (0.16, 0.10, 0.03))
]

grid_child_tf_ids = []

for idx, (title, sub, sym, accent_col, bg_col) in enumerate(app_icons_data):
    item_go_id = next_id()
    item_tf_id = next_id()
    item_btn_id = next_id()
    item_di_id = next_id()
    
    box_go_id = next_id()
    box_tf_id = next_id()
    box_cr_id = next_id()
    box_img_id = next_id()
    
    border_go_id = next_id()
    border_tf_id = next_id()
    border_cr_id = next_id()
    border_img_id = next_id()
    
    sym_go_id = next_id()
    sym_tf_id = next_id()
    sym_cr_id = next_id()
    sym_tmp_id = next_id()
    
    tl_go_id = next_id()
    tl_tf_id = next_id()
    tl_cr_id = next_id()
    tl_tmp_id = next_id()
    
    sl_go_id = next_id()
    sl_tf_id = next_id()
    sl_cr_id = next_id()
    sl_tmp_id = next_id()
    
    border_go_doc = make_gameobject(border_go_id, 'Border', [border_tf_id, border_cr_id, border_img_id])
    border_tf_doc = make_rect_transform(border_tf_id, border_go_id, box_tf_id, [], (0, 0), (1, 0), (0.5, 0), (0, 0), (0, 2.5))
    border_cr_doc = make_canvas_renderer(border_cr_id, border_go_id)
    border_img_doc = make_image(border_img_id, border_go_id, (accent_col[0], accent_col[1], accent_col[2], 0.7))
    
    sym_go_doc = make_gameobject(sym_go_id, 'Symbol', [sym_tf_id, sym_cr_id, sym_tmp_id])
    sym_tf_doc = make_rect_transform(sym_tf_id, sym_go_id, box_tf_id, [], (0, 0), (1, 1), (0.5, 0.5), (0, 0), (0, 0))
    sym_cr_doc = make_canvas_renderer(sym_cr_id, sym_go_id)
    sym_tmp_doc = make_tmp(sym_tmp_id, sym_go_id, sym, 14, (accent_col[0], accent_col[1], accent_col[2], 1.0), 2, True)
    
    box_children = [border_tf_id, sym_tf_id]
    box_go_doc = make_gameobject(box_go_id, 'IconBox', [box_tf_id, box_cr_id, box_img_id])
    box_tf_doc = make_rect_transform(box_tf_id, box_go_id, item_tf_id, box_children, (0.5, 1), (0.5, 1), (0.5, 1), (0, 0), (62, 62))
    box_cr_doc = make_canvas_renderer(box_cr_id, box_go_id)
    box_img_doc = make_image(box_img_id, box_go_id, (bg_col[0], bg_col[1], bg_col[2], 0.9))
    
    tl_go_doc = make_gameobject(tl_go_id, 'Title', [tl_tf_id, tl_cr_id, tl_tmp_id])
    tl_tf_doc = make_rect_transform(tl_tf_id, tl_go_id, item_tf_id, [], (0, 0), (1, 0), (0.5, 0), (0, 22), (0, 20))
    tl_cr_doc = make_canvas_renderer(tl_cr_id, tl_go_id)
    tl_tmp_doc = make_tmp(tl_tmp_id, tl_go_id, title, 13, (1, 1, 1, 1), 2, True)
    
    sl_go_doc = make_gameobject(sl_go_id, 'Sub', [sl_tf_id, sl_cr_id, sl_tmp_id])
    sl_tf_doc = make_rect_transform(sl_tf_id, sl_go_id, item_tf_id, [], (0, 0), (1, 0), (0.5, 0), (0, 4), (0, 18))
    sl_cr_doc = make_canvas_renderer(sl_cr_id, sl_go_id)
    sl_tmp_doc = make_tmp(sl_tmp_id, sl_go_id, sub, 10, (0.6, 0.68, 0.82, 1.0), 2, False)
    
    item_children = [box_tf_id, tl_tf_id, sl_tf_id]
    item_cr_id = next_id()
    item_img_id = next_id()
    item_cr_doc = make_canvas_renderer(item_cr_id, item_go_id)
    item_img_doc = make_image(item_img_id, item_go_id, (0, 0, 0, 0))
    item_go_doc = make_gameobject(item_go_id, f'Icon_{title}', [item_tf_id, item_cr_id, item_img_id, item_btn_id, item_di_id])
    item_tf_doc = make_rect_transform(item_tf_id, item_go_id, grid_tf_id, item_children, (0.5, 1), (0.5, 1), (0.5, 1), (0, -idx * 126), (120, 110))
    item_btn_doc = make_button(item_btn_id, item_go_id, box_img_id)
    item_di_doc = make_desktop_icon(item_di_id, item_go_id, title, sub, box_img_id, border_img_id, tl_tmp_id, sl_tmp_id, accent_col)
    
    grid_child_tf_ids.append(item_tf_id)
    
    generated_docs.extend([
        item_go_doc, item_tf_doc, item_cr_doc, item_img_doc, item_btn_doc, item_di_doc,
        box_go_doc, box_tf_doc, box_cr_doc, box_img_doc,
        border_go_doc, border_tf_doc, border_cr_doc, border_img_doc,
        sym_go_doc, sym_tf_doc, sym_cr_doc, sym_tmp_doc,
        tl_go_doc, tl_tf_doc, tl_cr_doc, tl_tmp_doc,
        sl_go_doc, sl_tf_doc, sl_cr_doc, sl_tmp_doc
    ])

grid_go_doc = make_gameobject(grid_go_id, 'DesktopGrid', [grid_tf_id, grid_vl_id])
grid_tf_doc = make_rect_transform(grid_tf_id, grid_go_id, '734257990', grid_child_tf_ids, (0, 1), (0, 1), (0, 1), (40, -80), (130, 760))
grid_vl_doc = make_v_layout(grid_vl_id, grid_go_id, 16, 1)

generated_docs.extend([grid_go_doc, grid_tf_doc, grid_vl_doc])

# --- 4. SystemMetricsCard (Top-Right) ---
mc_go_id = next_id()
mc_tf_id = next_id()
mc_cr_id = next_id()
mc_img_id = next_id()

mch_go_id = next_id()
mch_tf_id = next_id()
mch_cr_id = next_id()
mch_tmp_id = next_id()

mch_go_doc = make_gameobject(mch_go_id, 'Header', [mch_tf_id, mch_cr_id, mch_tmp_id])
mch_tf_doc = make_rect_transform(mch_tf_id, mch_go_id, mc_tf_id, [], (0, 1), (1, 1), (0.5, 1), (0, -12), (-28, 26))
mch_cr_doc = make_canvas_renderer(mch_cr_id, mch_go_id)
mch_tmp_doc = make_tmp(mch_tmp_id, mch_go_id, 'SYSTEM TELEMETRY', 12, (0.0, 1.0, 1.0, 1.0), 1, True)

def make_metric_bar(parent_tf_id, parent_name, label_text, default_val, y_offset, fill_color, fill_amount):
    cont_go_id = next_id()
    cont_tf_id = next_id()
    
    lbl_go_id = next_id()
    lbl_tf_id = next_id()
    lbl_cr_id = next_id()
    lbl_tmp_id = next_id()
    
    val_go_id = next_id()
    val_tf_id = next_id()
    val_cr_id = next_id()
    val_tmp_id = next_id()
    
    tr_go_id = next_id()
    tr_tf_id = next_id()
    tr_cr_id = next_id()
    tr_img_id = next_id()
    
    fl_go_id = next_id()
    fl_tf_id = next_id()
    fl_cr_id = next_id()
    fl_img_id = next_id()
    
    lbl_go_doc = make_gameobject(lbl_go_id, 'Label', [lbl_tf_id, lbl_cr_id, lbl_tmp_id])
    lbl_tf_doc = make_rect_transform(lbl_tf_id, lbl_go_id, cont_tf_id, [], (0, 0.5), (0.5, 1), (0, 1), (0, 0), (0, 20))
    lbl_cr_doc = make_canvas_renderer(lbl_cr_id, lbl_go_id)
    lbl_tmp_doc = make_tmp(lbl_tmp_id, lbl_go_id, label_text, 11, (0.75, 0.82, 0.95, 1.0), 1, True)
    
    val_go_doc = make_gameobject(val_go_id, 'Value', [val_tf_id, val_cr_id, val_tmp_id])
    val_tf_doc = make_rect_transform(val_tf_id, val_go_id, cont_tf_id, [], (0.5, 0.5), (1, 1), (1, 1), (0, 0), (0, 20))
    val_cr_doc = make_canvas_renderer(val_cr_id, val_go_id)
    val_tmp_doc = make_tmp(val_tmp_id, val_go_id, default_val, 11, (fill_color[0], fill_color[1], fill_color[2], 1.0), 4, True)
    
    fl_go_doc = make_gameobject(fl_go_id, 'Fill', [fl_tf_id, fl_cr_id, fl_img_id])
    fl_tf_doc = make_rect_transform(fl_tf_id, fl_go_id, tr_tf_id, [], (0, 0), (1, 1), (0.5, 0.5), (0, 0), (0, 0))
    fl_cr_doc = make_canvas_renderer(fl_cr_id, fl_go_id)
    fl_img_doc = make_image(fl_img_id, fl_go_id, (fill_color[0], fill_color[1], fill_color[2], 1.0), True, fill_amount)
    
    tr_go_doc = make_gameobject(tr_go_id, 'Track', [tr_tf_id, tr_cr_id, tr_img_id])
    tr_tf_doc = make_rect_transform(tr_tf_id, tr_go_id, cont_tf_id, [fl_tf_id], (0, 0), (1, 0), (0.5, 0), (0, 0), (0, 8))
    tr_cr_doc = make_canvas_renderer(tr_cr_id, tr_go_id)
    tr_img_doc = make_image(tr_img_id, tr_go_id, (0.1, 0.15, 0.24, 0.8))
    
    cont_children = [lbl_tf_id, val_tf_id, tr_tf_id]
    cont_go_doc = make_gameobject(cont_go_id, parent_name, [cont_tf_id])
    cont_tf_doc = make_rect_transform(cont_tf_id, cont_go_id, parent_tf_id, cont_children, (0, 1), (1, 1), (0.5, 1), (0, y_offset), (-28, 46))
    
    docs = [
        cont_go_doc, cont_tf_doc,
        lbl_go_doc, lbl_tf_doc, lbl_cr_doc, lbl_tmp_doc,
        val_go_doc, val_tf_doc, val_cr_doc, val_tmp_doc,
        tr_go_doc, tr_tf_doc, tr_cr_doc, tr_img_doc,
        fl_go_doc, fl_tf_doc, fl_cr_doc, fl_img_doc
    ]
    return cont_tf_id, docs, val_tmp_id, fl_img_id

cpu_tf_id, cpu_docs, cpu_val_tmp_id, cpu_fill_img_id = make_metric_bar(mc_tf_id, 'CPU_Metric', 'CPU USAGE', '18%', -46, (0.0, 1.0, 1.0), 0.18)
ram_tf_id, ram_docs, ram_val_tmp_id, ram_fill_img_id = make_metric_bar(mc_tf_id, 'RAM_Metric', 'MEMORY (RAM)', '5.8 / 16.0 GB (36%)', -98, (0.6, 0.1, 1.0), 0.36)
gpu_tf_id, gpu_docs, gpu_val_tmp_id, gpu_fill_img_id = make_metric_bar(mc_tf_id, 'GPU_Metric', 'GPU COMPUTE', '27%', -150, (1.0, 0.0, 0.5), 0.27)

# NetworkIO
nio_go_id = next_id()
nio_tf_id = next_id()
nio_cr_id = next_id()
nio_tmp_id = next_id()
nio_go_doc = make_gameobject(nio_go_id, 'NetworkIO', [nio_tf_id, nio_cr_id, nio_tmp_id])
nio_tf_doc = make_rect_transform(nio_tf_id, nio_go_id, mc_tf_id, [], (0, 0), (1, 0), (0.5, 0), (0, 12), (-28, 20))
nio_cr_doc = make_canvas_renderer(nio_cr_id, nio_go_id)
nio_tmp_doc = make_tmp(nio_tmp_id, nio_go_id, 'NETWORK I/O:  ↑ 1.8 MB/s   ↓ 54.2 MB/s', 11, (0.68, 0.76, 0.90, 1.0), 2, False)

mc_children = [mch_tf_id, cpu_tf_id, ram_tf_id, gpu_tf_id, nio_tf_id]
mc_go_doc = make_gameobject(mc_go_id, 'SystemMetricsCard', [mc_tf_id, mc_cr_id, mc_img_id])
mc_tf_doc = make_rect_transform(mc_tf_id, mc_go_id, '734257990', mc_children, (1, 1), (1, 1), (1, 1), (-40, -80), (340, 230))
mc_cr_doc = make_canvas_renderer(mc_cr_id, mc_go_id)
mc_img_doc = make_image(mc_img_id, mc_go_id, (0.06, 0.09, 0.15, 0.92))

generated_docs.extend([
    mc_go_doc, mc_tf_doc, mc_cr_doc, mc_img_doc,
    mch_go_doc, mch_tf_doc, mch_cr_doc, mch_tmp_doc
])
generated_docs.extend(cpu_docs)
generated_docs.extend(ram_docs)
generated_docs.extend(gpu_docs)
generated_docs.extend([nio_go_doc, nio_tf_doc, nio_cr_doc, nio_tmp_doc])

# --- 5. HostInfoCard (Below SystemMetrics) ---
hc_go_id = next_id()
hc_tf_id = next_id()
hc_cr_id = next_id()
hc_img_id = next_id()

hch_go_id = next_id()
hch_tf_id = next_id()
hch_cr_id = next_id()
hch_tmp_id = next_id()

hch_go_doc = make_gameobject(hch_go_id, 'HostHeader', [hch_tf_id, hch_cr_id, hch_tmp_id])
hch_tf_doc = make_rect_transform(hch_tf_id, hch_go_id, hc_tf_id, [], (0, 1), (1, 1), (0.5, 1), (0, -12), (-28, 26))
hch_cr_doc = make_canvas_renderer(hch_cr_id, hch_go_id)
hch_tmp_doc = make_tmp(hch_tmp_id, hch_go_id, 'REMOTE LINK PROFILE', 12, (0.6, 0.1, 1.0, 1.0), 1, True)

host_details = [
    "Target Host: 192.168.1.105:8080",
    "Protocol: Low Latency WebRTC / H.265",
    "Audio Stream: 48 kHz Stereo 24-bit",
    "Encryption: TLS 1.3 / AES-256 GCM",
    "Render Sync: VSync 60 FPS (0 Frame Drop)"
]

hc_children = [hch_tf_id]
hc_detail_docs = []

for idx, txt in enumerate(host_details):
    det_go_id = next_id()
    det_tf_id = next_id()
    det_cr_id = next_id()
    det_tmp_id = next_id()
    
    det_go_doc = make_gameobject(det_go_id, f'Detail_{idx}', [det_tf_id, det_cr_id, det_tmp_id])
    det_tf_doc = make_rect_transform(det_tf_id, det_go_id, hc_tf_id, [], (0, 1), (1, 1), (0.5, 1), (0, -42 - (idx * 22)), (-28, 20))
    det_cr_doc = make_canvas_renderer(det_cr_id, det_go_id)
    det_tmp_doc = make_tmp(det_tmp_id, det_go_id, txt, 11, (0.72, 0.80, 0.92, 1.0), 1, False)
    
    hc_children.append(det_tf_id)
    hc_detail_docs.extend([det_go_doc, det_tf_doc, det_cr_doc, det_tmp_doc])

hc_go_doc = make_gameobject(hc_go_id, 'HostInfoCard', [hc_tf_id, hc_cr_id, hc_img_id])
hc_tf_doc = make_rect_transform(hc_tf_id, hc_go_id, '734257990', hc_children, (1, 1), (1, 1), (1, 1), (-40, -330), (340, 160))
hc_cr_doc = make_canvas_renderer(hc_cr_id, hc_go_id)
hc_img_doc = make_image(hc_img_id, hc_go_id, (0.06, 0.09, 0.15, 0.92))

generated_docs.extend([
    hc_go_doc, hc_tf_doc, hc_cr_doc, hc_img_doc,
    hch_go_doc, hch_tf_doc, hch_cr_doc, hch_tmp_doc
])
generated_docs.extend(hc_detail_docs)

# --- 6. BottomDock (Centered, Floating at y = 24) ---
dock_go_id = next_id()
dock_tf_id = next_id()
dock_cr_id = next_id()
dock_img_id = next_id()
dock_hl_id = next_id()

drim_go_id = next_id()
drim_tf_id = next_id()
drim_cr_id = next_id()
drim_img_id = next_id()

drim_go_doc = make_gameobject(drim_go_id, 'Dock_Rim', [drim_tf_id, drim_cr_id, drim_img_id])
drim_tf_doc = make_rect_transform(drim_tf_id, drim_go_id, dock_tf_id, [], (0, 1), (1, 1), (0.5, 1), (0, 0), (0, 2))
drim_cr_doc = make_canvas_renderer(drim_cr_id, drim_go_id)
drim_img_doc = make_image(drim_img_id, drim_go_id, (0.0, 1.0, 1.0, 0.5))

dock_apps = [
    ("Home", (0.0, 1.0, 1.0), True),
    ("Files", (0.6, 0.1, 1.0), False),
    ("Browser", (0.0, 1.0, 1.0), False),
    ("Terminal", (0.2, 0.9, 0.4), False),
    ("Settings", (1.0, 0.6, 0.1), False),
    ("Connection", (1.0, 0.0, 0.5), False)
]

dock_child_tf_ids = [drim_tf_id]
dock_app_docs = []

dock_x_positions = [-235, -141, -47, 47, 141, 235]

for idx, (app_name, app_col, is_active) in enumerate(dock_apps):
    dapp_go_id = next_id()
    dapp_tf_id = next_id()
    dapp_btn_id = next_id()
    dapp_di_id = next_id()
    
    pbg_go_id = next_id()
    pbg_tf_id = next_id()
    pbg_cr_id = next_id()
    pbg_img_id = next_id()
    
    prim_go_id = next_id()
    prim_tf_id = next_id()
    prim_cr_id = next_id()
    prim_img_id = next_id()
    
    plbl_go_id = next_id()
    plbl_tf_id = next_id()
    plbl_cr_id = next_id()
    plbl_tmp_id = next_id()
    
    dot_go_id = next_id()
    dot_tf_id = next_id()
    dot_cr_id = next_id()
    dot_img_id = next_id()
    
    prim_go_doc = make_gameobject(prim_go_id, 'Rim', [prim_tf_id, prim_cr_id, prim_img_id])
    prim_tf_doc = make_rect_transform(prim_tf_id, prim_go_id, pbg_tf_id, [], (0, 0), (1, 0), (0.5, 0), (0, 0), (0, 2))
    prim_cr_doc = make_canvas_renderer(prim_cr_id, prim_go_id)
    prim_img_doc = make_image(prim_img_id, prim_go_id, (app_col[0], app_col[1], app_col[2], 1.0))
    
    plbl_go_doc = make_gameobject(plbl_go_id, 'Label', [plbl_tf_id, plbl_cr_id, plbl_tmp_id])
    plbl_tf_doc = make_rect_transform(plbl_tf_id, plbl_go_id, pbg_tf_id, [], (0, 0), (1, 1), (0.5, 0.5), (0, 0), (0, 0))
    plbl_cr_doc = make_canvas_renderer(plbl_cr_id, plbl_go_id)
    plbl_tmp_doc = make_tmp(plbl_tmp_id, plbl_go_id, app_name.upper(), 9, (1, 1, 1, 1), 2, True)
    
    pbg_children = [prim_tf_id, plbl_tf_id]
    pbg_go_doc = make_gameobject(pbg_go_id, 'Bg', [pbg_tf_id, pbg_cr_id, pbg_img_id])
    pbg_tf_doc = make_rect_transform(pbg_tf_id, pbg_go_id, dapp_tf_id, pbg_children, (0.5, 0.5), (0.5, 0.5), (0.5, 0.5), (0, 4), (52, 52))
    pbg_cr_doc = make_canvas_renderer(pbg_cr_id, pbg_go_id)
    pbg_img_doc = make_image(pbg_img_id, pbg_go_id, (0.08, 0.12, 0.20, 0.9))
    
    dot_go_doc = make_gameobject(dot_go_id, 'ActiveDot', [dot_tf_id, dot_cr_id, dot_img_id])
    dot_tf_doc = make_rect_transform(dot_tf_id, dot_go_id, dapp_tf_id, [], (0.5, 0), (0.5, 0), (0.5, 0), (0, -2), (5, 5))
    dot_cr_doc = make_canvas_renderer(dot_cr_id, dot_go_id)
    dot_img_doc = make_image(dot_img_id, dot_go_id, (app_col[0], app_col[1], app_col[2], 1.0 if is_active else 0.0))
    
    dapp_children = [pbg_tf_id, dot_tf_id]
    dapp_cr_id = next_id()
    dapp_img_id = next_id()
    dapp_cr_doc = make_canvas_renderer(dapp_cr_id, dapp_go_id)
    dapp_img_doc = make_image(dapp_img_id, dapp_go_id, (0, 0, 0, 0))
    dapp_go_doc = make_gameobject(dapp_go_id, f'Dock_{app_name}', [dapp_tf_id, dapp_cr_id, dapp_img_id, dapp_btn_id, dapp_di_id])
    dapp_tf_doc = make_rect_transform(dapp_tf_id, dapp_go_id, dock_tf_id, dapp_children, (0.5, 0.5), (0.5, 0.5), (0.5, 0.5), (dock_x_positions[idx], 0), (74, 64))
    dapp_btn_doc = make_button(dapp_btn_id, dapp_go_id, pbg_img_id)
    dapp_di_doc = make_desktop_icon(dapp_di_id, dapp_go_id, app_name, '', pbg_img_id, prim_img_id, plbl_tmp_id, None, app_col, dot_go_id)
    
    dock_child_tf_ids.append(dapp_tf_id)
    
    dock_app_docs.extend([
        dapp_go_doc, dapp_tf_doc, dapp_cr_doc, dapp_img_doc, dapp_btn_doc, dapp_di_doc,
        pbg_go_doc, pbg_tf_doc, pbg_cr_doc, pbg_img_doc,
        prim_go_doc, prim_tf_doc, prim_cr_doc, prim_img_doc,
        plbl_go_doc, plbl_tf_doc, plbl_cr_doc, plbl_tmp_doc,
        dot_go_doc, dot_tf_doc, dot_cr_doc, dot_img_doc
    ])

dock_go_doc = make_gameobject(dock_go_id, 'BottomDock', [dock_tf_id, dock_cr_id, dock_img_id, dock_hl_id])
dock_tf_doc = make_rect_transform(dock_tf_id, dock_go_id, '734257990', dock_child_tf_ids, (0.5, 0), (0.5, 0), (0.5, 0), (0, 24), (640, 80))
dock_cr_doc = make_canvas_renderer(dock_cr_id, dock_go_id)
dock_img_doc = make_image(dock_img_id, dock_go_id, (0.05, 0.08, 0.14, 0.94))
dock_hl_doc = make_h_layout(dock_hl_id, dock_go_id, 20, 4)

generated_docs.extend([
    dock_go_doc, dock_tf_doc, dock_cr_doc, dock_img_doc, dock_hl_doc,
    drim_go_doc, drim_tf_doc, drim_cr_doc, drim_img_doc
])
generated_docs.extend(dock_app_docs)

# --- 7. VRCursor (Preserve Exact IDs 416825115, 416825116, 416825117, 416825118, 416825119) ---
cur_dot_go_id = next_id()
cur_dot_tf_id = next_id()
cur_dot_cr_id = next_id()
cur_dot_img_id = next_id()

cur_dot_go_doc = make_gameobject(cur_dot_go_id, 'CenterDot', [cur_dot_tf_id, cur_dot_cr_id, cur_dot_img_id])
cur_dot_tf_doc = make_rect_transform(cur_dot_tf_id, cur_dot_go_id, '416825116', [], (0.5, 0.5), (0.5, 0.5), (0.5, 0.5), (0, 0), (8, 8))
cur_dot_cr_doc = make_canvas_renderer(cur_dot_cr_id, cur_dot_go_id)
cur_dot_img_doc = make_image(cur_dot_img_id, cur_dot_go_id, (1, 1, 1, 1))

cur_go_doc = f'''1 &416825115
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: 416825116}}
  - component: {{fileID: 416825119}}
  - component: {{fileID: 416825118}}
  - component: {{fileID: 416825117}}
  m_Layer: 5
  m_Name: VRCursor
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
'''

cur_tf_doc = f'''224 &416825116
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 416825115}}
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children:
  - {{fileID: {cur_dot_tf_id}}}
  m_Father: {{fileID: 734257990}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
  m_AnchorMin: {{x: 0.5, y: 0.5}}
  m_AnchorMax: {{x: 0.5, y: 0.5}}
  m_AnchoredPosition: {{x: 0, y: 0}}
  m_SizeDelta: {{x: 28, y: 28}}
  m_Pivot: {{x: 0.5, y: 0.5}}
'''

cur_cr_doc = f'''222 &416825119
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 416825115}}
  m_CullTransparentMesh: 1
'''

cur_img_doc = f'''114 &416825118
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 416825115}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {IMAGE_GUID}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Image
  m_Material: {{fileID: 0}}
  m_Color: {{r: 0, g: 1, b: 1, a: 1}}
  m_RaycastTarget: 0
  m_RaycastPadding: {{x: 0, y: 0, z: 0, w: 0}}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: {{fileID: 0}}
  m_Type: 0
  m_PreserveAspect: 0
  m_FillCenter: 1
  m_FillMethod: 4
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0
  m_PixelsPerUnitMultiplier: 1
'''

cur_comp_doc = f'''114 &416825117
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 416825115}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {CURSOR_GUID}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::VRRemoteDesktop.Input.VRDesktopCursor
  _canvasRect: {{fileID: 734257990}}
  _cursorImage: {{fileID: 416825118}}
  _normalColor: {{r: 0, g: 1, b: 1, a: 1}}
  _hoverColor: {{r: 1, g: 1, b: 1, a: 1}}
  _clickColor: {{r: 1, g: 0, b: 0.5, a: 1}}
'''

generated_docs.extend([
    cur_go_doc, cur_tf_doc, cur_cr_doc, cur_img_doc, cur_comp_doc,
    cur_dot_go_doc, cur_dot_tf_doc, cur_dot_cr_doc, cur_dot_img_doc
])

# --- 8. Update DesktopCanvas Transform and DesktopUIManager ---
canvas_children_tf_ids = [bg_tf_id, topbar_tf_id, grid_tf_id, mc_tf_id, hc_tf_id, dock_tf_id, '416825116']

updated_canvas_tf = f'''224 &734257990
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 734257989}}
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 0, y: 0, z: 0}}
  m_ConstrainProportionsScale: 0
  m_Children:
{chr(10).join([f"  - {{fileID: {cid}}}" for cid in canvas_children_tf_ids])}
  m_Father: {{fileID: 1626253454}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
  m_AnchorMin: {{x: 0, y: 0}}
  m_AnchorMax: {{x: 0, y: 0}}
  m_AnchoredPosition: {{x: 0, y: 0}}
  m_SizeDelta: {{x: 0, y: 0}}
  m_Pivot: {{x: 0.5, y: 0.5}}
'''

updated_uimanager = f'''114 &734257991
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 734257989}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: 28171a1baf97b1e4cb32c9f0e352bfd7, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::VRRemoteDesktop.UI.DesktopUIManager
  _timeText: {{fileID: {tim_tmp_id}}}
  _dateText: {{fileID: {dat_tmp_id}}}
  _connectionText: {{fileID: {cbt_tmp_id}}}
  _latencyText: {{fileID: {lat_tmp_id}}}
  _computerNameText: {{fileID: {cn_tmp_id}}}
  _networkStatusText: {{fileID: {ns_tmp_id}}}
  _resolutionText: {{fileID: {res_tmp_id}}}
  _settingsButton: {{fileID: {set_btn_id}}}
  _cpuText: {{fileID: {cpu_val_tmp_id}}}
  _cpuBarFill: {{fileID: {cpu_fill_img_id}}}
  _ramText: {{fileID: {ram_val_tmp_id}}}
  _ramBarFill: {{fileID: {ram_fill_img_id}}}
  _gpuText: {{fileID: {gpu_val_tmp_id}}}
  _gpuBarFill: {{fileID: {gpu_fill_img_id}}}
  _networkIOText: {{fileID: {nio_tmp_id}}}
  _background: {{fileID: {bg_img_id}}}
  _topBarBackground: {{fileID: {topbar_img_id}}}
  _dockBackground: {{fileID: {dock_img_id}}}
  _telemetryWidgetBg: {{fileID: {mc_img_id}}}
  _hostInfoWidgetBg: {{fileID: {hc_img_id}}}
'''

# Put updated canvas and uimanager in doc_map
doc_map['734257990'] = ('224', updated_canvas_tf)
doc_map['734257991'] = ('114', updated_uimanager)

# Rebuild scene docs
final_docs = []
for f_id in doc_order:
    if f_id in old_canvas_children_ids:
        continue # removed
    t_id, d = doc_map[f_id]
    final_docs.append(f'--- !u!{d.strip()}\n')

# Append all new generated docs
for d in generated_docs:
    final_docs.append(f'--- !u!{d.strip()}\n')

final_content = header + ''.join(final_docs)

with open(SCENE_PATH, 'w', encoding='utf-8') as f:
    f.write(final_content)

print(f'Successfully built Phase 4 UI in {SCENE_PATH}!')
print(f'Total new documents appended: {len(generated_docs)}')

# Verify reference integrity
defined_ids = set()
for m in re.finditer(r'--- !u!\d+\s+&(\d+)', final_content):
    defined_ids.add(m.group(1))

local_refs = re.findall(r'\{fileID:\s*(\d+)\}', final_content)
missing = [r for r in local_refs if r != '0' and r not in defined_ids]
print(f'Integrity Check: {len(missing)} missing references.')
if missing:
    print('Missing IDs:', set(missing))
else:
    print('ALL REFERENCES 100% RESOLVED! ZERO ERRORS!')
