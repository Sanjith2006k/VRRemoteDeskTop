import sys
sys.stdout.reconfigure(encoding='utf-8')
import re

with open('Assets/Scenes/Main.unity', 'r', encoding='utf-8') as f:
    content = f.read()

go_names = {}
tf_data = {}
for block in content.split('--- !u!'):
    if not block.strip(): continue
    lines = block.strip().split('\n')
    m = re.match(r'(\d+)\s+&(\d+)', lines[0])
    if not m: continue
    c_type, f_id = m.groups()
    if c_type == '1':
        name = ''
        for l in lines:
            if 'm_Name:' in l: name = l.split('m_Name:')[1].strip()
        go_names[f_id] = name
    elif c_type == '224':
        m_go = ''
        m_father = ''
        m_children = []
        m_anchoredPosition = [0.0, 0.0]
        m_sizeDelta = [0.0, 0.0]
        m_anchorMin = [0.5, 0.5]
        m_anchorMax = [0.5, 0.5]
        m_pivot = [0.5, 0.5]
        for l in lines:
            if 'm_GameObject:' in l:
                m_go = re.search(r'fileID:\s*(\d+)', l).group(1)
            elif 'm_Father:' in l:
                m_father = re.search(r'fileID:\s*(\d+)', l).group(1)
            elif '- {fileID:' in l:
                m_c = re.search(r'fileID:\s*(\d+)', l)
                if m_c: m_children.append(m_c.group(1))
            elif 'm_AnchoredPosition:' in l:
                p = re.findall(r'[-+]?\d*\.?\d+', l)
                if len(p) >= 2: m_anchoredPosition = [float(p[0]), float(p[1])]
            elif 'm_SizeDelta:' in l:
                p = re.findall(r'[-+]?\d*\.?\d+', l)
                if len(p) >= 2: m_sizeDelta = [float(p[0]), float(p[1])]
            elif 'm_AnchorMin:' in l:
                p = re.findall(r'[-+]?\d*\.?\d+', l)
                if len(p) >= 2: m_anchorMin = [float(p[0]), float(p[1])]
            elif 'm_AnchorMax:' in l:
                p = re.findall(r'[-+]?\d*\.?\d+', l)
                if len(p) >= 2: m_anchorMax = [float(p[0]), float(p[1])]
            elif 'm_Pivot:' in l:
                p = re.findall(r'[-+]?\d*\.?\d+', l)
                if len(p) >= 2: m_pivot = [float(p[0]), float(p[1])]
        tf_data[f_id] = {
            'go': m_go, 'father': m_father, 'children': m_children,
            'pos': m_anchoredPosition, 'size': m_sizeDelta,
            'amin': m_anchorMin, 'amax': m_anchorMax, 'pivot': m_pivot
        }

def get_rect(tf_id, parent_rect):
    d = tf_data[tf_id]
    px, py, pw, ph = parent_rect
    amin_x, amin_y = d['amin']
    amax_x, amax_y = d['amax']
    pos_x, pos_y = d['pos']
    s_x, s_y = d['size']
    piv_x, piv_y = d['pivot']
    
    anchor_min_x = px + amin_x * pw
    anchor_max_x = px + amax_x * pw
    anchor_min_y = py + amin_y * ph
    anchor_max_y = py + amax_y * ph
    
    w = (anchor_max_x - anchor_min_x) + s_x
    h = (anchor_max_y - anchor_min_y) + s_y
    
    pivot_pt_x = anchor_min_x + (anchor_max_x - anchor_min_x) * piv_x + pos_x
    pivot_pt_y = anchor_min_y + (anchor_max_y - anchor_min_y) * piv_y + pos_y
    
    x = pivot_pt_x - piv_x * w
    y = pivot_pt_y - piv_y * h
    return (x, y, w, h)

canvas_rect = (0.0, 0.0, 1920.0, 1080.0)

def print_sub_tree(tf_id, parent_rect, depth=0):
    r = get_rect(tf_id, parent_rect)
    go = tf_data[tf_id]['go']
    name = go_names.get(go, "?")
    indent = "  " * depth
    print(f"{indent}{name:20} : X={r[0]:6.1f}..{r[0]+r[2]:6.1f} | Y={r[1]:6.1f}..{r[1]+r[3]:6.1f} | W={r[2]:5.1f} H={r[3]:5.1f}")
    for ch in tf_data[tf_id]['children']:
        print_sub_tree(ch, r, depth + 1)

print("=== DETAILED UI TREE RECTANGLES ===")
for ch in tf_data['734257990']['children']:
    go = tf_data[ch]['go']
    name = go_names.get(go, "?")
    if name in ['Background', 'VRCursor']: continue
    print(f"\n--- {name} ---")
    print_sub_tree(ch, canvas_rect)
