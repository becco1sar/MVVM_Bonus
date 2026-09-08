#!/usr/bin/env python3
"""Static checks for a WPF project that cannot be compiled in this environment.

0. Every XAML file is well-formed XML, and no property is set both as an
   attribute and as a <Tag.Property> child (XAML rejects a property set twice).
1. Every {StaticResource key} used in a view resolves to a key defined in that
   view, App.xaml, or Resources/Styles.xaml.
2. Every key used inside Styles.xaml is defined before its first use there
   (StaticResource is resolved in document order).
3. Every event handler named in XAML exists in the code-behind, and every
   element the code-behind touches by name has a matching x:Name.
4. The first segment of every {Binding Path} maps to a public member on the
   view model bound to that view, or onto some model class in the project.
"""
import io, os, re, sys, glob
import xml.etree.ElementTree as ET

ROOT = sys.argv[1]
os.chdir(ROOT)

def read(p):
    return io.open(p, encoding='utf-8-sig', errors='replace').read()

VIEWS = sorted(glob.glob('View/*.xaml')) + ['MainWindow.xaml']
DICTS = ['Resources/Styles.xaml', 'App.xaml']

KEY_RE = re.compile(r'x:Key="([^"]+)"')
USE_RE = re.compile(r'\{StaticResource\s+([A-Za-z_][\w.]*)\s*\}')

problems = []

# ---------- 0: well-formedness + duplicate property sets ----------
def local(tag):
    return tag.split('}')[-1] if '}' in tag else tag

for f in VIEWS + DICTS:
    try:
        tree = ET.parse(f)
    except Exception as e:
        # "--" inside a comment is the usual cause, and a hard XAML build failure.
        problems.append(f"{f}: not well-formed XML: {e}")
        continue

    for parent in tree.iter():
        ptag = local(parent.tag)
        attrs = {local(a) for a in parent.attrib}
        for child in list(parent):
            ctag = local(child.tag)
            if ctag.startswith(ptag + '.'):
                prop = ctag[len(ptag) + 1:]
                if prop in attrs:
                    problems.append(
                        f"{f}: '{prop}' set as an attribute and as <{ctag}> on the same {ptag}")

# ---------- 1 & 2: resource keys ----------
global_keys = {}
for d in DICTS:
    for m in KEY_RE.finditer(read(d)):
        global_keys.setdefault(m.group(1), d)

styles = read('Resources/Styles.xaml')
defined_at = {m.group(1): m.start() for m in KEY_RE.finditer(styles)}
for m in USE_RE.finditer(styles):
    k, pos = m.group(1), m.start()
    if k in defined_at and defined_at[k] > pos:
        line = styles[:pos].count('\n') + 1
        problems.append(f"Resources/Styles.xaml:{line}: '{k}' used before it is defined")

for v in VIEWS:
    txt = read(v)
    local_keys = set(KEY_RE.findall(txt))
    for m in USE_RE.finditer(txt):
        k = m.group(1)
        if k not in local_keys and k not in global_keys:
            line = txt[:m.start()].count('\n') + 1
            problems.append(f"{v}:{line}: unresolved StaticResource '{k}'")

# ---------- 3: handlers and names vs code-behind ----------
EVENT_RE = re.compile(r'\b(?:Click|MouseLeftButtonDown|SelectionChanged|TextChanged|Checked|'
                      r'Unchecked|Loaded|MouseDoubleClick|KeyDown|GotFocus|LostFocus)="([A-Za-z_]\w*)"')
NAME_RE = re.compile(r'x:Name="([A-Za-z_]\w*)"')
TOUCH_RE = re.compile(r'\b([A-Z]\w*)\.(?:Width|Opacity|Focus|SelectAll|BeginAnimation|Visibility|Tag|ToolTip)\b')
SKIP = {'Grid', 'GridLength', 'Visibility', 'Double', 'WindowState', 'System', 'Dispatcher'}

for v in VIEWS:
    cb = v + '.cs'
    if not os.path.exists(cb):
        continue
    xaml, cs = read(v), read(cb)
    names = set(NAME_RE.findall(xaml))
    for h in sorted(set(EVENT_RE.findall(xaml))):
        if not re.search(r'\b' + re.escape(h) + r'\s*\(', cs):
            problems.append(f"{v}: handler '{h}' has no method in {cb}")
    for used in sorted(set(TOUCH_RE.findall(cs))):
        if used not in SKIP and used not in names:
            problems.append(f"{cb}: touches '{used}', which is not an x:Name in {v}")

# ---------- 4: bindings vs view-model members ----------
app = read('App.xaml')
vm_for_view = {'MainWindow.xaml': 'MainViewModel'}
for m in re.finditer(r'DataType="\{x:Type vm:(\w+)\}"\s*>\s*<v:(\w+)\s*/>', app):
    vm_for_view['View/%s.xaml' % m.group(2)] = m.group(1)

MEMBER_RE = re.compile(r'public\s+(?:static\s+)?(?:virtual\s+|override\s+|async\s+|readonly\s+)?'
                       r'[\w<>,\[\]?\.]+\s+(\w+)\s*(?:\{|=>|;)')

def members_of(path):
    return set(MEMBER_RE.findall(read(path))) if os.path.exists(path) else set()

model_members = set()
for f in glob.glob('Model/*.cs') + glob.glob('ViewModel/*.cs') + glob.glob('Services/*.cs'):
    model_members |= members_of(f)

BIND_RE = re.compile(r'\{Binding\s+([^},]*)')
for v in VIEWS:
    vmname = vm_for_view.get(v)
    if not vmname:
        continue
    vm_members = members_of('ViewModel/%s.cs' % vmname)
    if not vm_members:
        problems.append(f"{v}: cannot locate ViewModel/{vmname}.cs")
        continue
    txt = read(v)
    for m in BIND_RE.finditer(txt):
        expr = m.group(1).strip()
        if not expr or expr.startswith(('Path=', 'RelativeSource', 'ElementName')):
            continue
        if '=' in expr.split()[0]:
            continue
        # A binding resolved against something other than the DataContext says
        # nothing about the view model, so skip it wherever the qualifier appears.
        closing = txt.find('}', m.start())
        full = txt[m.start():closing + 1] if closing != -1 else expr
        if any(q in full for q in ('RelativeSource', 'ElementName', 'Source=')):
            continue
        first = re.split(r'[.\[]', expr)[0].strip()
        if not re.match(r'^[A-Za-z_]\w*$', first or ''):
            continue
        if first not in vm_members and first not in model_members:
            line = txt[:m.start()].count('\n') + 1
            problems.append(f"{v}:{line}: binding '{expr}' -> '{first}' not found on {vmname} or any model")

print(f"checked {len(VIEWS)} views, {len(global_keys)} shared resource keys")
if problems:
    print(f"\n{len(problems)} problem(s):")
    for p in problems:
        print("  -", p)
    sys.exit(1)
print("\nno problems found")
