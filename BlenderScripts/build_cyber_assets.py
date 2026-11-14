import bpy
import math
import os
from mathutils import Vector

BASE_DIR = r"C:\Users\akeer.AKERU\.gemini\antigravity\scratch\cyber-rush-unity"
MODELS_DIR = os.path.join(BASE_DIR, "UnityProject", "Assets", "Models")
BLENDER_DIR = os.path.join(BASE_DIR, "BlenderScripts")
os.makedirs(MODELS_DIR, exist_ok=True)


def clear_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def get_principled(mat):
    for n in mat.node_tree.nodes:
        if n.type == 'BSDF_PRINCIPLED':
            return n
    mat.node_tree.nodes.clear()
    out = mat.node_tree.nodes.new("ShaderNodeOutputMaterial")
    bsdf = mat.node_tree.nodes.new("ShaderNodeBsdfPrincipled")
    mat.node_tree.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    return bsdf


def make_mat(name, color, rough=0.35, metallic=0.6, emission=None, emission_strength=0.0):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    b = get_principled(m)
    b.inputs["Base Color"].default_value = color
    b.inputs["Roughness"].default_value = rough
    b.inputs["Metallic"].default_value = metallic
    if emission:
        b.inputs["Emission Color"].default_value = emission
        b.inputs["Emission Strength"].default_value = emission_strength
    return m


def create_materials():
    mats = {}
    mats["obsidian"] = make_mat("Cyber_Obsidian", (0.06, 0.07, 0.11, 1.0), rough=0.28, metallic=0.75)
    mats["titanium"] = make_mat("Cyber_Titanium", (0.26, 0.30, 0.38, 1.0), rough=0.22, metallic=0.92)
    mats["blade_steel"] = make_mat("Cyber_BladeSteel", (0.72, 0.80, 0.90, 1.0), rough=0.14, metallic=0.96)
    mats["gold"] = make_mat("Cyber_Gold", (0.95, 0.72, 0.22, 1.0), rough=0.18, metallic=0.95)
    mats["glow_cyan"] = make_mat("Glow_Cyan", (0.12, 0.95, 1.0, 1.0), rough=0.10, metallic=0.1, emission=(0.12, 0.95, 1.0, 1.0), emission_strength=9.0)
    mats["glow_magenta"] = make_mat("Glow_Magenta", (1.0, 0.16, 0.68, 1.0), rough=0.10, metallic=0.1, emission=(1.0, 0.16, 0.68, 1.0), emission_strength=9.0)
    mats["glow_crimson"] = make_mat("Glow_Crimson", (1.0, 0.14, 0.18, 1.0), rough=0.10, metallic=0.1, emission=(1.0, 0.14, 0.18, 1.0), emission_strength=9.0)
    mats["glow_gold"] = make_mat("Glow_Gold", (1.0, 0.78, 0.20, 1.0), rough=0.10, metallic=0.2, emission=(1.0, 0.78, 0.20, 1.0), emission_strength=8.5)
    return mats


def set_smooth(obj):
    if obj.type == 'MESH':
        for p in obj.data.polygons:
            p.use_smooth = True


def add_bevel(obj, w=0.012, segs=2):
    m = obj.modifiers.new("Bevel", 'BEVEL')
    m.width = w
    m.segments = segs
    m.limit_method = 'ANGLE'
    m.angle_limit = math.radians(28)


def hbox(objs, name, size, loc, mat, bevel=0.01, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=loc, rotation=rot)
    o = bpy.context.active_object
    o.name = name
    o.scale = size
    bpy.ops.object.transform_apply(scale=True)
    if bevel > 0:
        add_bevel(o, bevel, 2)
    o.data.materials.append(mat)
    objs.append(o)
    return o


def hcyl(objs, name, r, d, loc, mat, segs=16, bevel=0.008, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cylinder_add(vertices=segs, radius=r, depth=d, location=loc, rotation=rot)
    o = bpy.context.active_object
    o.name = name
    set_smooth(o)
    if bevel > 0:
        add_bevel(o, bevel, 2)
    o.data.materials.append(mat)
    objs.append(o)
    return o


def hcone(objs, name, r1, r2, d, loc, mat, segs=8, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cone_add(vertices=segs, radius1=r1, radius2=r2, depth=d, location=loc, rotation=rot)
    o = bpy.context.active_object
    o.name = name
    o.data.materials.append(mat)
    objs.append(o)
    return o


def export_fbx(objs, filepath):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.export_scene.fbx(
        filepath=filepath,
        use_selection=True,
        apply_scale_options='FBX_SCALE_ALL',
        axis_forward='-Z',
        axis_up='Y',
        bake_anim=False,
        add_leaf_bones=False,
    )


# --------------------------------------------------------------------------
# 1. CYBER KATANA (Grip at origin, Blade extending forward along -Y -> Unity +Z)
# --------------------------------------------------------------------------
def build_cyber_katana(mats):
    objs = []
    # Tsuka Grip & Diamond Cyber Ribs
    hbox(objs, "Katana_Grip", (0.032, 0.28, 0.044), (0, 0.05, 0), mats["obsidian"], bevel=0.006)
    for i in range(5):
        hbox(objs, f"Katana_GripRing_{i}", (0.036, 0.016, 0.048), (0, -0.05 + i * 0.05, 0), mats["glow_cyan"], bevel=0.002)
    hbox(objs, "Katana_Pommel", (0.042, 0.045, 0.054), (0, 0.20, 0), mats["gold"], bevel=0.008)

    # Cyber Tsuba (Split Vented Handguard)
    hbox(objs, "Katana_Tsuba_Main", (0.095, 0.026, 0.13), (0, -0.10, 0), mats["titanium"], bevel=0.006)
    hbox(objs, "Katana_Tsuba_VentL", (0.11, 0.014, 0.024), (0, -0.10, 0.035), mats["glow_cyan"], bevel=0.002)
    hbox(objs, "Katana_Tsuba_VentR", (0.11, 0.014, 0.024), (0, -0.10, -0.035), mats["glow_cyan"], bevel=0.002)

    # Segmented Blade Spine (Back edge) + Monomolecular Neon Cutting Edge
    hbox(objs, "Katana_Spine", (0.014, 0.86, 0.026), (0, -0.54, 0.016), mats["obsidian"], bevel=0.003)
    hbox(objs, "Katana_Body", (0.011, 0.86, 0.028), (0, -0.54, 0.0), mats["blade_steel"], bevel=0.002)
    hbox(objs, "Katana_NeonEdge", (0.006, 0.85, 0.012), (0, -0.54, -0.016), mats["glow_cyan"], bevel=0.001)
    # Energy Spine Circuit Windows
    for j in range(4):
        hbox(objs, f"Katana_Slot_{j}", (0.017, 0.09, 0.008), (0, -0.24 - j * 0.17, 0.014), mats["glow_cyan"], bevel=0.001)

    # Sharp Angled Tanto Tip aligned cleanly along blade width
    hcone(objs, "Katana_Tip_Steel", 0.014, 0.001, 0.16, (0, -1.04, 0.004), mats["blade_steel"], segs=4, rot=(math.radians(-90), 0, math.radians(45)))
    bpy.context.active_object.scale = (0.48, 1.0, 1.45)
    bpy.ops.object.transform_apply(scale=True)
    hcone(objs, "Katana_Tip_Neon", 0.011, 0.001, 0.16, (0, -1.04, -0.010), mats["glow_cyan"], segs=4, rot=(math.radians(-90), 0, math.radians(45)))
    bpy.context.active_object.scale = (0.32, 1.0, 1.05)
    bpy.ops.object.transform_apply(scale=True)
    return objs


# --------------------------------------------------------------------------
# 2. CYBER INTERCEPTOR DRONE (Faces -Y)
# --------------------------------------------------------------------------
def build_cyber_drone(mats):
    objs = []
    # Main Stealth Diamond Core
    hbox(objs, "Drone_Chassis", (0.82, 1.05, 0.42), (0, 0, 1.35), mats["obsidian"], bevel=0.03)
    hbox(objs, "Drone_TopPlate", (0.58, 0.82, 0.14), (0, 0.05, 1.58), mats["titanium"], bevel=0.02)
    # Crimson Cyclops Optics + Brow Hood
    hbox(objs, "Drone_Visor", (0.48, 0.16, 0.14), (0, -0.50, 1.36), mats["glow_crimson"], bevel=0.01)
    hbox(objs, "Drone_Brow", (0.56, 0.24, 0.08), (0, -0.46, 1.46), mats["titanium"], bevel=0.01, rot=(0.22, 0, 0))

    # Swept Wing Fins + Magenta Neon Strips
    for sgn in [-1, 1]:
        hbox(objs, f"Drone_Wing_{sgn}", (0.78, 0.48, 0.08), (sgn * 0.72, 0.12, 1.38), mats["obsidian"], bevel=0.015, rot=(0.1, sgn * 0.25, sgn * -0.35))
        hbox(objs, f"Drone_WingNeon_{sgn}", (0.68, 0.08, 0.095), (sgn * 0.72, -0.02, 1.38), mats["glow_magenta"], bevel=0.005, rot=(0.1, sgn * 0.25, sgn * -0.35))
        hcyl(objs, f"Drone_Thruster_{sgn}", 0.14, 0.42, (sgn * 0.34, 0.56, 1.35), mats["titanium"], segs=12, rot=(math.radians(90), 0, 0))
        hcyl(objs, f"Drone_ThrusterCore_{sgn}", 0.09, 0.10, (sgn * 0.34, 0.76, 1.35), mats["glow_cyan"], segs=12, rot=(math.radians(90), 0, 0))
    return objs


# --------------------------------------------------------------------------
# 3. CYBER ENFORCER TROOPER (Armored Mecha Sentinel facing -Y)
# --------------------------------------------------------------------------
def build_cyber_trooper(mats):
    objs = []
    # Greaves & Thighs
    for sgn in [-1, 1]:
        hbox(objs, f"Trooper_Leg_{sgn}", (0.24, 0.28, 0.92), (sgn * 0.22, 0, 0.46), mats["obsidian"], bevel=0.025)
        hbox(objs, f"Trooper_KneePad_{sgn}", (0.22, 0.14, 0.22), (sgn * 0.22, -0.14, 0.54), mats["titanium"], bevel=0.015)

    # Torso + Reactor Core + Belt
    hbox(objs, "Trooper_Pelvis", (0.58, 0.36, 0.28), (0, 0, 1.02), mats["titanium"], bevel=0.02)
    hbox(objs, "Trooper_Torso", (0.74, 0.48, 0.68), (0, 0, 1.48), mats["obsidian"], bevel=0.03)
    hbox(objs, "Trooper_ChestReactor", (0.24, 0.12, 0.24), (0, -0.23, 1.54), mats["glow_crimson"], bevel=0.01, rot=(0, math.radians(45), 0))

    # Angular Samurai-Cyber Helmet
    hbox(objs, "Trooper_Head", (0.38, 0.40, 0.40), (0, -0.02, 2.02), mats["obsidian"], bevel=0.03)
    hbox(objs, "Trooper_Visor", (0.34, 0.12, 0.10), (0, -0.20, 2.02), mats["glow_crimson"], bevel=0.01)
    hbox(objs, "Trooper_Crest", (0.08, 0.46, 0.14), (0, 0.02, 2.24), mats["titanium"], bevel=0.01)

    # Heavy Pauldrons + Dual Energy Tonfa-Blades
    for sgn in [-1, 1]:
        hbox(objs, f"Trooper_Pauldron_{sgn}", (0.38, 0.44, 0.28), (sgn * 0.52, 0, 1.72), mats["titanium"], bevel=0.025, rot=(0, sgn * 0.25, 0))
        hbox(objs, f"Trooper_Arm_{sgn}", (0.20, 0.24, 0.62), (sgn * 0.54, -0.10, 1.32), mats["obsidian"], bevel=0.02, rot=(0.35, 0, 0))
        hbox(objs, f"Trooper_Blade_{sgn}", (0.06, 0.72, 0.12), (sgn * 0.56, -0.44, 1.15), mats["glow_magenta"], bevel=0.008)
    return objs


# --------------------------------------------------------------------------
# 4. ARCHON CORE BOSS (Monumental Rotating Reactor)
# --------------------------------------------------------------------------
def build_cyber_core_boss(mats):
    objs = []
    # Central Pulsing Magenta Prism
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=1.05, location=(0, 0, 2.6))
    core = bpy.context.active_object
    core.name = "Boss_EnergyHeart"
    core.data.materials.append(mats["glow_magenta"])
    objs.append(core)

    # Gyroscope Gold Rings
    for idx, (r, rot) in enumerate([(1.75, (0.45, 0.2, 0)), (2.25, (-0.35, 0.6, 0.3)), (2.75, (0.85, -0.4, 0.5))]):
        bpy.ops.mesh.primitive_torus_add(major_radius=r, minor_radius=0.10, major_segments=32, minor_segments=8, location=(0, 0, 2.6), rotation=rot)
        ring = bpy.context.active_object
        ring.name = f"Boss_GyroRing_{idx}"
        set_smooth(ring)
        ring.data.materials.append(mats["gold"] if idx != 1 else mats["titanium"])
        objs.append(ring)

    # 4 Armored Shield Pylons Framing Core
    for i in range(4):
        a = i * (math.pi / 2)
        px, py = math.cos(a) * 2.1, math.sin(a) * 2.1
        hbox(objs, f"Boss_ShieldWing_{i}", (0.32, 0.95, 2.6), (px, py, 2.6), mats["obsidian"], bevel=0.03, rot=(0, 0, a))
        hbox(objs, f"Boss_ShieldNeon_{i}", (0.36, 0.18, 2.2), (px, py, 2.6), mats["glow_crimson"], bevel=0.01, rot=(0, 0, a))
    return objs


# --------------------------------------------------------------------------
# 5. JUMP PAD, GRAPPLE ANCHOR, GOAL PORTAL
# --------------------------------------------------------------------------
def build_jump_pad(mats):
    objs = []
    hcyl(objs, "JumpPad_Base", 1.55, 0.26, (0, 0, 0.13), mats["obsidian"], segs=8, bevel=0.025)
    hcyl(objs, "JumpPad_Rim", 1.38, 0.30, (0, 0, 0.16), mats["titanium"], segs=8, bevel=0.015)
    hcyl(objs, "JumpPad_Core", 1.05, 0.34, (0, 0, 0.18), mats["glow_cyan"], segs=8, bevel=0.01)
    return objs


def build_grapple_anchor(mats):
    objs = []
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=0.45, location=(0, 0, 0))
    c = bpy.context.active_object
    c.name = "Grapple_Beacon"
    c.data.materials.append(mats["glow_gold"])
    objs.append(c)

    bpy.ops.mesh.primitive_torus_add(major_radius=0.82, minor_radius=0.08, major_segments=24, minor_segments=6, location=(0, 0, 0), rotation=(math.radians(90), 0, 0))
    r1 = bpy.context.active_object
    r1.name = "Grapple_RingOuter"
    r1.data.materials.append(mats["obsidian"])
    objs.append(r1)

    bpy.ops.mesh.primitive_torus_add(major_radius=0.68, minor_radius=0.05, major_segments=24, minor_segments=6, location=(0, 0, 0), rotation=(0, math.radians(45), 0))
    r2 = bpy.context.active_object
    r2.name = "Grapple_RingGlow"
    r2.data.materials.append(mats["glow_cyan"])
    objs.append(r2)
    return objs


def build_goal_portal(mats):
    objs = []
    # Monumental Cyber Torii Archway (7.6m wide, 8.5m tall)
    for sgn in [-1, 1]:
        hbox(objs, f"Portal_Pillar_{sgn}", (1.15, 1.35, 7.6), (sgn * 3.4, 0, 3.8), mats["obsidian"], bevel=0.05)
        hbox(objs, f"Portal_Trim_{sgn}", (0.25, 1.42, 7.2), (sgn * 2.8, 0, 3.8), mats["glow_cyan"], bevel=0.02)
    hbox(objs, "Portal_TopBeam", (9.8, 1.75, 1.15), (0, 0, 8.0), mats["obsidian"], bevel=0.05)
    hbox(objs, "Portal_TopCrown", (11.2, 1.45, 0.55), (0, 0, 8.85), mats["titanium"], bevel=0.04)
    hbox(objs, "Portal_TopNeon", (9.2, 1.82, 0.22), (0, 0, 8.0), mats["glow_magenta"], bevel=0.01)
    hbox(objs, "Portal_BaseThreshold", (8.2, 2.2, 0.32), (0, 0, 0.16), mats["glow_cyan"], bevel=0.02)
    return objs


# --------------------------------------------------------------------------
# 8. CYBER BLASTER (Grip near origin, Rail barrel pointing -Y -> Unity +Z)
# --------------------------------------------------------------------------
def build_cyber_blaster(mats):
    objs = []
    # Ergonomic angled grip & gold magazine cap
    hbox(objs, "Blaster_Grip", (0.046, 0.075, 0.16), (0, 0.045, -0.055), mats["obsidian"], bevel=0.008, rot=(math.radians(-16), 0, 0))
    hbox(objs, "Blaster_MagCap", (0.050, 0.082, 0.028), (0, 0.068, -0.135), mats["gold"], bevel=0.005, rot=(math.radians(-16), 0, 0))
    hbox(objs, "Blaster_TriggerGuard", (0.028, 0.085, 0.018), (0, -0.035, -0.045), mats["titanium"], bevel=0.004)
    hbox(objs, "Blaster_Trigger", (0.012, 0.022, 0.032), (0, -0.02, -0.015), mats["glow_magenta"], bevel=0.002)

    # Main receiver chassis & upper titanium shroud
    hbox(objs, "Blaster_Receiver", (0.068, 0.34, 0.092), (0, -0.10, 0.045), mats["obsidian"], bevel=0.010)
    hbox(objs, "Blaster_UpperSlide", (0.074, 0.31, 0.044), (0, -0.09, 0.095), mats["titanium"], bevel=0.008)
    hbox(objs, "Blaster_RearVent", (0.078, 0.06, 0.055), (0, 0.06, 0.072), mats["gold"], bevel=0.006)

    # Dual Magnetic Railgun Barrels + Glowing Plasma Channel
    hbox(objs, "Blaster_TopRail", (0.042, 0.34, 0.028), (0, -0.36, 0.082), mats["titanium"], bevel=0.005)
    hbox(objs, "Blaster_BottomRail", (0.044, 0.31, 0.032), (0, -0.345, 0.024), mats["obsidian"], bevel=0.005)
    hbox(objs, "Blaster_PlasmaCore", (0.024, 0.36, 0.024), (0, -0.34, 0.054), mats["glow_cyan"], bevel=0.003)
    hbox(objs, "Blaster_MuzzleRing", (0.052, 0.036, 0.082), (0, -0.52, 0.054), mats["blade_steel"], bevel=0.005)

    # Side energy vents
    for sgn in [-1, 1]:
        for k in range(3):
            hbox(objs, f"Blaster_SideCell_{sgn}_{k}", (0.008, 0.055, 0.018), (sgn * 0.038, -0.06 - k * 0.08, 0.052), mats["glow_cyan"], bevel=0.002)

    # Holographic Reflex Sight
    hbox(objs, "Blaster_HoloBase", (0.046, 0.09, 0.022), (0, -0.04, 0.125), mats["obsidian"], bevel=0.004)
    hbox(objs, "Blaster_HoloArchL", (0.008, 0.05, 0.048), (-0.022, -0.04, 0.155), mats["titanium"], bevel=0.002)
    hbox(objs, "Blaster_HoloArchR", (0.008, 0.05, 0.048), (0.022, -0.04, 0.155), mats["titanium"], bevel=0.002)
    hbox(objs, "Blaster_HoloTop", (0.052, 0.05, 0.010), (0, -0.04, 0.180), mats["titanium"], bevel=0.002)
    hbox(objs, "Blaster_HoloDot", (0.012, 0.004, 0.012), (0, -0.04, 0.156), mats["glow_magenta"], bevel=0.001)
    return objs


# --------------------------------------------------------------------------
# 9. CYBER NINJA BOT (VS CPU & PvP Opponent Avatar, 1.82m tall, facing -Y)
# --------------------------------------------------------------------------
def build_cyber_ninja_bot(mats):
    objs = []
    # Boots & Armored Legs (Z up in Blender)
    for sgn in [-1, 1]:
        x = sgn * 0.15
        hbox(objs, f"Bot_Boot_{sgn}", (0.14, 0.25, 0.11), (x, -0.03, 0.055), mats["obsidian"], bevel=0.015)
        hbox(objs, f"Bot_BootGlow_{sgn}", (0.145, 0.04, 0.03), (x, -0.14, 0.055), mats["glow_cyan"], bevel=0.004)
        hbox(objs, f"Bot_Shin_{sgn}", (0.13, 0.15, 0.44), (x, 0.0, 0.34), mats["titanium"], bevel=0.018)
        hbox(objs, f"Bot_KneePad_{sgn}", (0.14, 0.09, 0.11), (x, -0.07, 0.54), mats["obsidian"], bevel=0.012)
        hbox(objs, f"Bot_Thigh_{sgn}", (0.16, 0.18, 0.42), (x, 0.0, 0.77), mats["obsidian"], bevel=0.020)

    # Pelvis & Waist Reactor Belt
    hbox(objs, "Bot_Pelvis", (0.38, 0.24, 0.18), (0, 0, 0.99), mats["obsidian"], bevel=0.020)
    hbox(objs, "Bot_BeltGlow", (0.40, 0.26, 0.05), (0, -0.01, 1.07), mats["glow_magenta"], bevel=0.006)

    # Armored Cyber Torso & Chest Core
    hbox(objs, "Bot_TorsoLower", (0.36, 0.23, 0.24), (0, 0, 1.18), mats["obsidian"], bevel=0.018)
    hbox(objs, "Bot_ChestArmor", (0.46, 0.28, 0.28), (0, -0.01, 1.38), mats["titanium"], bevel=0.022)
    hbox(objs, "Bot_ChestCore", (0.14, 0.06, 0.14), (0, -0.14, 1.39), mats["glow_cyan"], bevel=0.008)
    hbox(objs, "Bot_BackPack", (0.28, 0.14, 0.30), (0, 0.18, 1.38), mats["obsidian"], bevel=0.018)
    hbox(objs, "Bot_BackThrusterL", (0.07, 0.07, 0.20), (-0.09, 0.24, 1.37), mats["glow_cyan"], bevel=0.006, rot=(math.radians(18), 0, 0))
    hbox(objs, "Bot_BackThrusterR", (0.07, 0.07, 0.20), (0.09, 0.24, 1.37), mats["glow_cyan"], bevel=0.006, rot=(math.radians(18), 0, 0))

    # Shoulder Pauldrons & Cyber Scarf Tails
    for sgn in [-1, 1]:
        hbox(objs, f"Bot_Pauldron_{sgn}", (0.20, 0.26, 0.15), (sgn * 0.30, 0, 1.49), mats["obsidian"], bevel=0.016, rot=(0, sgn * math.radians(14), 0))
        hbox(objs, f"Bot_PauldronTrim_{sgn}", (0.21, 0.06, 0.04), (sgn * 0.31, -0.08, 1.52), mats["glow_magenta"], bevel=0.004)
        # Upper & Lower Arms in Combat Stance
        hbox(objs, f"Bot_UpperArm_{sgn}", (0.12, 0.13, 0.30), (sgn * 0.31, -0.02, 1.30), mats["obsidian"], bevel=0.015)
        hbox(objs, f"Bot_Forearm_{sgn}", (0.11, 0.28, 0.11), (sgn * 0.29, -0.17, 1.15), mats["titanium"], bevel=0.012)
        hbox(objs, f"Bot_ArmBlade_{sgn}", (0.02, 0.34, 0.05), (sgn * 0.35, -0.28, 1.14), mats["glow_cyan"], bevel=0.003)

    # Flowing Angular Cyber Scarf
    hbox(objs, "Bot_ScarfCollar", (0.28, 0.26, 0.09), (0, 0.02, 1.54), mats["glow_magenta"], bevel=0.010)
    hbox(objs, "Bot_ScarfTail1", (0.14, 0.45, 0.03), (-0.06, 0.32, 1.40), mats["glow_magenta"], bevel=0.004, rot=(math.radians(32), 0, math.radians(-12)))
    hbox(objs, "Bot_ScarfTail2", (0.11, 0.56, 0.025), (0.07, 0.36, 1.32), mats["obsidian"], bevel=0.004, rot=(math.radians(38), 0, math.radians(14)))

    # Cyber Ninja Helmet + Oni Antenna + Neon Visor
    hbox(objs, "Bot_Helmet", (0.24, 0.27, 0.26), (0, -0.01, 1.70), mats["obsidian"], bevel=0.028)
    hbox(objs, "Bot_Visor", (0.21, 0.08, 0.065), (0, -0.12, 1.70), mats["glow_cyan"], bevel=0.006)
    hbox(objs, "Bot_Mask", (0.18, 0.10, 0.10), (0, -0.11, 1.61), mats["titanium"], bevel=0.010)
    for sgn in [-1, 1]:
        hbox(objs, f"Bot_Horn_{sgn}", (0.03, 0.14, 0.16), (sgn * 0.11, -0.02, 1.84), mats["gold"], bevel=0.005, rot=(math.radians(-22), sgn * math.radians(18), 0))
    return objs


def main():
    builders = [
        ("Cyber_Katana.fbx", build_cyber_katana),
        ("Cyber_Drone.fbx", build_cyber_drone),
        ("Cyber_Trooper.fbx", build_cyber_trooper),
        ("Cyber_CoreBoss.fbx", build_cyber_core_boss),
        ("Jump_Pad.fbx", build_jump_pad),
        ("Grapple_Anchor.fbx", build_grapple_anchor),
        ("Goal_Portal.fbx", build_goal_portal),
        ("Cyber_Blaster.fbx", build_cyber_blaster),
        ("Cyber_NinjaBot.fbx", build_cyber_ninja_bot),
    ]

    for fname, fn in builders:
        clear_scene()
        mats = create_materials()
        objs = fn(mats)
        export_fbx(objs, os.path.join(MODELS_DIR, fname))

    # Build Showcase Beauty Scene & Cycles Render
    clear_scene()
    mats = create_materials()
    all_objs = []
    offsets = [
        (-4.8, 0, 0.6, build_cyber_katana),
        (-3.2, -0.8, 0.9, build_cyber_blaster),
        (-2.0, -1.0, 0.0, build_cyber_drone),
        (0.0, -1.0, 0.0, build_cyber_trooper),
        (2.0, -0.8, 0.0, build_cyber_ninja_bot),
        (4.4, 1.2, 0.0, build_cyber_core_boss),
        (-3.4, 2.5, 0.0, build_jump_pad),
        (0.5, 2.5, 2.2, build_grapple_anchor),
        (0.0, 7.0, 0.0, build_goal_portal),
    ]
    for ox, oy, oz, fn in offsets:
        parts = fn(mats)
        for p in parts:
            p.location.x += ox
            p.location.y += oy
            p.location.z += oz
        all_objs.extend(parts)

    # Dark neon grid floor for showcase
    bpy.ops.mesh.primitive_plane_add(size=40.0, location=(0, 2, 0))
    fl = bpy.context.active_object
    fl.data.materials.append(mats["obsidian"])

    scene = bpy.context.scene
    scene.render.engine = 'CYCLES'
    prefs = bpy.context.preferences.addons['cycles'].preferences
    prefs.compute_device_type = 'OPTIX'
    prefs.refresh_devices()
    for d in prefs.devices:
        d.use = (d.type == 'OPTIX')
    scene.cycles.device = 'GPU'
    scene.cycles.samples = 48
    scene.cycles.use_denoising = True
    scene.render.resolution_x = 1280
    scene.render.resolution_y = 720

    world = bpy.data.worlds.new("CyberNight")
    scene.world = world
    world.use_nodes = True
    nt = world.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputWorld")
    bg = nt.nodes.new("ShaderNodeBackground")
    bg.inputs["Color"].default_value = (0.015, 0.02, 0.06, 1.0)
    bg.inputs["Strength"].default_value = 1.4
    nt.links.new(bg.outputs["Background"], out.inputs["Surface"])

    light_data = bpy.data.lights.new("KeyLight", type='SUN')
    light = bpy.data.objects.new("KeyLight", light_data)
    scene.collection.objects.link(light)
    light.rotation_euler = (math.radians(48), 0, math.radians(-30))
    light_data.energy = 2.4

    cam_data = bpy.data.cameras.new("PreviewCam")
    cam = bpy.data.objects.new("PreviewCam", cam_data)
    scene.collection.objects.link(cam)
    cam.location = (0.0, -7.8, 3.6)
    cam.rotation_euler = (Vector((0.0, 2.0, 1.9)) - cam.location).to_track_quat('-Z', 'Y').to_euler()
    scene.camera = cam

    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(BLENDER_DIR, "cyber_assets.blend"))
    scene.render.filepath = os.path.join(BLENDER_DIR, "preview_cyber_assets.png")
    bpy.ops.render.render(write_still=True)
    print("CYBER RUSH BLENDER ASSETS EXPORTED SUCCESSFULLY!")


if __name__ == "__main__":
    main()
