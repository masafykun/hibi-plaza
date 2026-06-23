import math
import sys
from pathlib import Path

import bpy


OUTPUT_DIR = Path(sys.argv[sys.argv.index("--") + 1]).resolve() if "--" in sys.argv else Path.cwd()
OUTPUT_DIR.mkdir(parents=True, exist_ok=True)


PALETTE = {
    "skin": (1.0, 0.68, 0.50, 1.0),
    "skin_shadow": (0.78, 0.38, 0.25, 1.0),
    "hair": (0.13, 0.055, 0.028, 1.0),
    "top": (0.10, 0.38, 0.86, 1.0),
    "top_dark": (0.035, 0.16, 0.42, 1.0),
    "bottom": (0.08, 0.14, 0.24, 1.0),
    "white": (0.94, 0.96, 0.98, 1.0),
    "ink": (0.018, 0.012, 0.02, 1.0),
    "blush": (1.0, 0.18, 0.30, 1.0),
    "gold": (1.0, 0.55, 0.08, 1.0),
    "stone": (0.70, 0.64, 0.56, 1.0),
    "stone_light": (0.92, 0.85, 0.73, 1.0),
    "wood": (0.35, 0.12, 0.045, 1.0),
    "wood_light": (0.66, 0.28, 0.08, 1.0),
    "green": (0.12, 0.48, 0.20, 1.0),
    "green_light": (0.32, 0.70, 0.26, 1.0),
    "metal": (0.035, 0.055, 0.065, 1.0),
    "water": (0.08, 0.67, 0.92, 0.76),
}


def reset_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for collection in list(bpy.data.collections):
        if collection.users == 0:
            bpy.data.collections.remove(collection)


def material(name, color, metallic=0.0, roughness=0.48, emission=0.0):
    existing = bpy.data.materials.get(name)
    if existing:
        return existing
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = color
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = color
        bsdf.inputs["Roughness"].default_value = roughness
        bsdf.inputs["Metallic"].default_value = metallic
        emission_input = bsdf.inputs.get("Emission Color") or bsdf.inputs.get("Emission")
        if emission_input:
            emission_input.default_value = color
        strength_input = bsdf.inputs.get("Emission Strength")
        if strength_input:
            strength_input.default_value = emission
        if color[3] < 1.0:
            mat.surface_render_method = "DITHERED"
            bsdf.inputs["Alpha"].default_value = color[3]
    return mat


def assign(obj, mat):
    if obj.data and hasattr(obj.data, "materials"):
        obj.data.materials.append(mat)
    return obj


def smooth(obj):
    if obj.type == "MESH":
        for polygon in obj.data.polygons:
            polygon.use_smooth = True
    return obj


def empty(name, location=(0, 0, 0), parent=None):
    obj = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    obj.parent = parent
    return obj


def sphere(name, location, scale, mat, parent=None, segments=24, rings=16):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.parent = parent
    assign(obj, mat)
    return smooth(obj)


def rounded_cube(name, location, scale, mat, bevel=0.12, parent=None, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.scale = (scale[0] * 0.5, scale[1] * 0.5, scale[2] * 0.5)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel > 0:
        modifier = obj.modifiers.new("Soft edges", "BEVEL")
        modifier.width = bevel
        modifier.segments = 3
        modifier.limit_method = "ANGLE"
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=modifier.name)
    obj.parent = parent
    assign(obj, mat)
    return smooth(obj)


def cylinder(name, location, radius, depth, mat, parent=None, vertices=24, rotation=(0, 0, 0), bevel=0.05):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    if bevel > 0:
        modifier = obj.modifiers.new("Rounded rim", "BEVEL")
        modifier.width = bevel
        modifier.segments = 2
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=modifier.name)
    obj.parent = parent
    assign(obj, mat)
    return smooth(obj)


def cone(name, location, radius1, radius2, depth, mat, parent=None, vertices=24, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_cone_add(vertices=vertices, radius1=radius1, radius2=radius2, depth=depth, location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.parent = parent
    assign(obj, mat)
    return smooth(obj)


def torus(name, location, major_radius, minor_radius, mat, parent=None, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_torus_add(major_radius=major_radius, minor_radius=minor_radius, major_segments=32, minor_segments=10, location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.parent = parent
    assign(obj, mat)
    return smooth(obj)


def export_fbx(filename):
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.fbx(
        filepath=str(OUTPUT_DIR / filename),
        use_selection=False,
        object_types={"EMPTY", "MESH"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="AUTO",
    )


def build_avatar():
    root = empty("HibiAvatar")
    skin = material("Skin", PALETTE["skin"], roughness=0.58)
    skin_shadow = material("SkinShadow", PALETTE["skin_shadow"], roughness=0.62)
    hair = material("Hair", PALETTE["hair"], roughness=0.30)
    top = material("Top", PALETTE["top"], roughness=0.38)
    top_dark = material("TopDark", PALETTE["top_dark"], roughness=0.40)
    bottom = material("Bottom", PALETTE["bottom"], roughness=0.52)
    white = material("White", PALETTE["white"], roughness=0.34)
    ink = material("Ink", PALETTE["ink"], roughness=0.22)
    blush = material("Blush", PALETTE["blush"], roughness=0.68)
    gold = material("AccentGold", PALETTE["gold"], metallic=0.15, roughness=0.26)

    sphere("Skin Head", (0, 0, 2.48), (0.86, 0.76, 0.91), skin, root, 32, 20)
    sphere("Skin Ear L", (-0.83, 0.0, 2.45), (0.16, 0.10, 0.24), skin, root)
    sphere("Skin Ear R", (0.83, 0.0, 2.45), (0.16, 0.10, 0.24), skin, root)
    cylinder("Skin Neck", (0, 0, 1.78), 0.22, 0.30, skin_shadow, root, bevel=0.05)

    rounded_cube("Top Jacket", (0, 0.02, 1.38), (1.16, 0.60, 0.98), top, 0.22, root)
    rounded_cube("Shirt", (0, -0.315, 1.43), (0.48, 0.08, 0.78), white, 0.04, root)
    rounded_cube("Top Lapel L", (-0.24, -0.37, 1.62), (0.22, 0.07, 0.46), top_dark, 0.03, root, (0, 0.18, 0.22))
    rounded_cube("Top Lapel R", (0.24, -0.37, 1.62), (0.22, 0.07, 0.46), top_dark, 0.03, root, (0, -0.18, -0.22))
    sphere("Top Button", (0, -0.38, 1.20), (0.055, 0.03, 0.055), gold, root)

    for side, x in (("L", -0.69), ("R", 0.69)):
        pivot = empty(f"ArmPivot_{side}", (x, 0, 1.65), root)
        sphere(f"Top Sleeve {side}", (0, 0, -0.18), (0.28, 0.29, 0.42), top, pivot)
        sphere(f"Skin Arm {side}", (0, -0.01, -0.62), (0.20, 0.20, 0.45), skin, pivot)
        sphere(f"Skin Hand {side}", (0, -0.04, -1.00), (0.23, 0.20, 0.23), skin, pivot)

    rounded_cube("Bottom Shorts", (0, 0.02, 0.83), (1.02, 0.58, 0.44), bottom, 0.16, root)
    for side, x in (("L", -0.28), ("R", 0.28)):
        rounded_cube(f"Bottom Leg {side}", (x, 0.02, 0.48), (0.43, 0.47, 0.60), bottom, 0.12, root)
        rounded_cube(f"Shoe {side}", (x, -0.13, 0.14), (0.51, 0.76, 0.28), white, 0.13, root)
        rounded_cube(f"Shoe Sole {side}", (x, -0.14, 0.055), (0.54, 0.79, 0.10), ink, 0.04, root)
        rounded_cube(f"Shoe Accent {side}", (x, -0.52, 0.18), (0.30, 0.08, 0.09), top, 0.025, root)

    for side, x in (("L", -0.30), ("R", 0.30)):
        sphere(f"Eye White {side}", (x, -0.695, 2.55), (0.22, 0.055, 0.29), white, root, 24, 16)
        sphere(f"Eye Iris {side}", (x, -0.746, 2.54), (0.132, 0.030, 0.20), ink, root, 24, 16)
        sphere(f"Eye Highlight {side}", (x - 0.045, -0.777, 2.64), (0.045, 0.016, 0.058), white, root, 16, 10)
        rounded_cube(f"Brow {side}", (x, -0.735, 2.88), (0.31, 0.035, 0.055), hair, 0.025, root, (0, 0, 0.10 if side == "L" else -0.10))
        sphere(f"Cheek {side}", (x * 1.65, -0.713, 2.31), (0.16, 0.025, 0.075), blush, root, 20, 12)
    sphere("Skin Nose", (0, -0.765, 2.40), (0.075, 0.042, 0.085), skin_shadow, root, 20, 12)
    sphere("Mouth", (0, -0.758, 2.22), (0.15, 0.023, 0.055), ink, root, 20, 12)
    sphere("Mouth Smile", (0, -0.780, 2.245), (0.10, 0.012, 0.033), skin, root, 20, 12)

    style0 = empty("HairStyle_0", parent=root)
    sphere("Hair Cap 0", (0, 0.03, 2.82), (0.91, 0.76, 0.66), hair, style0, 32, 20)
    sphere("Hair Bob L 0", (-0.66, 0.03, 2.47), (0.31, 0.46, 0.63), hair, style0)
    sphere("Hair Bob R 0", (0.66, 0.03, 2.47), (0.31, 0.46, 0.63), hair, style0)
    for i, x in enumerate((-0.52, -0.26, 0.0, 0.26, 0.52)):
        sphere(f"Hair Fringe 0 {i}", (x, -0.63, 2.78 - abs(x) * 0.18), (0.25, 0.13, 0.34), hair, style0)

    style1 = empty("HairStyle_1", parent=root)
    sphere("Hair Cap 1", (0, 0.03, 2.82), (0.91, 0.76, 0.66), hair, style1, 32, 20)
    sphere("Hair Bun L 1", (-0.70, 0.02, 3.12), (0.36, 0.32, 0.38), hair, style1)
    sphere("Hair Bun R 1", (0.70, 0.02, 3.12), (0.36, 0.32, 0.38), hair, style1)
    for i, x in enumerate((-0.46, -0.15, 0.16, 0.47)):
        sphere(f"Hair Fringe 1 {i}", (x, -0.63, 2.80 - abs(x) * 0.12), (0.27, 0.13, 0.31), hair, style1)

    style2 = empty("HairStyle_2", parent=root)
    sphere("Hair Cap 2", (0, 0.05, 2.80), (0.90, 0.75, 0.61), hair, style2, 32, 20)
    for i, x in enumerate((-0.62, -0.32, 0.0, 0.32, 0.62)):
        cone(f"Hair Spike 2 {i}", (x, -0.02, 3.27 + (0.08 if i == 2 else 0)), 0.27, 0.03, 0.68, hair, style2, 18, (0.10 * (i - 2), 0, -0.14 * (i - 2)))
    for i, x in enumerate((-0.48, -0.16, 0.16, 0.48)):
        sphere(f"Hair Fringe 2 {i}", (x, -0.62, 2.79), (0.29, 0.13, 0.31), hair, style2)

    style3 = empty("HairStyle_3", parent=root)
    sphere("Hair Cap 3", (0, 0.08, 2.83), (0.92, 0.76, 0.61), hair, style3, 32, 20)
    for i, x in enumerate((-0.48, -0.16, 0.16, 0.48)):
        sphere(f"Hair Fringe 3 {i}", (x, -0.63, 2.80 - abs(x) * 0.10), (0.29, 0.13, 0.30), hair, style3)
    rounded_cube("Accessory Hair Clip 3", (0.62, -0.66, 2.82), (0.16, 0.07, 0.36), gold, 0.05, style3, (0, 0, -0.38))
    return root


def build_fountain():
    root = empty("Fountain")
    stone = material("FountainStone", PALETTE["stone_light"], roughness=0.60)
    stone_dark = material("FountainTrim", PALETTE["stone"], roughness=0.65)
    water = material("Water", PALETTE["water"], roughness=0.14)
    gold = material("FountainGold", PALETTE["gold"], metallic=0.30, roughness=0.24)
    cylinder("Stone Plinth", (0, 0, 0.18), 3.35, 0.36, stone_dark, root, 48, bevel=0.12)
    cylinder("Stone Basin", (0, 0, 0.42), 3.05, 0.36, stone, root, 48, bevel=0.16)
    torus("Stone Basin Rim", (0, 0, 0.64), 2.82, 0.20, stone, root)
    cylinder("Water Lower", (0, 0, 0.62), 2.70, 0.07, water, root, 48, bevel=0.02)
    cylinder("Stone Column", (0, 0, 1.35), 0.56, 1.45, stone, root, 32, bevel=0.12)
    cylinder("Stone Middle Bowl", (0, 0, 2.02), 1.75, 0.26, stone, root, 48, bevel=0.10)
    torus("Stone Middle Rim", (0, 0, 2.17), 1.54, 0.13, stone_dark, root)
    cylinder("Water Middle", (0, 0, 2.17), 1.43, 0.055, water, root, 40, bevel=0.015)
    cylinder("Stone Crown", (0, 0, 2.76), 0.28, 1.25, stone, root, 28, bevel=0.08)
    sphere("Gold Finial", (0, 0, 3.48), (0.28, 0.28, 0.34), gold, root)
    for i in range(8):
        angle = i * math.tau / 8
        x, y = math.cos(angle) * 2.92, math.sin(angle) * 2.92
        sphere(f"Stone Rosette {i}", (x, y, 0.48), (0.18, 0.18, 0.18), stone_dark, root, 16, 10)
    return root


def build_tree():
    root = empty("Tree")
    bark = material("TreeBark", PALETTE["wood"], roughness=0.78)
    bark_light = material("TreeBarkLight", PALETTE["wood_light"], roughness=0.76)
    green = material("Leaves", PALETTE["green"], roughness=0.72)
    green_light = material("LeavesLight", PALETTE["green_light"], roughness=0.70)
    cylinder("Tree Trunk", (0, 0, 2.2), 0.56, 4.4, bark, root, 18, bevel=0.12)
    for i, (x, y, z, rz) in enumerate(((-0.55, 0.0, 3.5, 0.65), (0.52, 0.12, 3.6, -0.62), (0.0, 0.48, 3.8, 0.0))):
        cylinder(f"Tree Branch {i}", (x, y, z), 0.22, 2.1, bark_light, root, 14, (0, rz, 0), 0.05)
    clusters = ((0, 0, 5.5, 2.1), (-1.45, 0.1, 5.1, 1.55), (1.40, 0.2, 5.15, 1.60), (-0.65, 0.45, 6.45, 1.45), (0.85, -0.1, 6.35, 1.50))
    for i, (x, y, z, s) in enumerate(clusters):
        sphere(f"Leaves Cluster {i}", (x, y, z), (s, s * 0.85, s * 0.88), green_light if i % 2 else green, root, 24, 16)
    return root


def build_bench():
    root = empty("Bench")
    wood = material("BenchWood", PALETTE["wood_light"], roughness=0.55)
    wood_dark = material("BenchWoodDark", PALETTE["wood"], roughness=0.60)
    metal = material("BenchMetal", PALETTE["metal"], metallic=0.45, roughness=0.28)
    for i in range(4):
        rounded_cube(f"Bench Seat Slat {i}", (0, -0.43 + i * 0.28, 1.0), (3.8, 0.22, 0.16), wood if i % 2 else wood_dark, 0.07, root)
    for i in range(4):
        rounded_cube(f"Bench Back Slat {i}", (0, 0.42, 1.55 + i * 0.30), (3.8, 0.16, 0.20), wood if i % 2 else wood_dark, 0.07, root)
    for side, x in (("L", -1.62), ("R", 1.62)):
        rounded_cube(f"Bench Leg {side}", (x, 0, 0.52), (0.18, 0.70, 1.05), metal, 0.08, root)
        torus(f"Bench Arm {side}", (x, -0.10, 1.45), 0.42, 0.08, metal, root, (math.pi / 2, 0, 0))
    return root


def build_lamp():
    root = empty("Lamp")
    metal = material("LampMetal", PALETTE["metal"], metallic=0.55, roughness=0.22)
    gold = material("LampGold", PALETTE["gold"], metallic=0.36, roughness=0.22)
    glass = material("LampGlow", (1.0, 0.72, 0.23, 1.0), roughness=0.14, emission=2.4)
    cylinder("Lamp Base", (0, 0, 0.22), 0.48, 0.44, metal, root, 24, bevel=0.10)
    cylinder("Lamp Post", (0, 0, 2.25), 0.14, 4.2, metal, root, 20, bevel=0.04)
    torus("Lamp Collar", (0, 0, 4.10), 0.28, 0.09, gold, root)
    cone("Lamp Housing", (0, 0, 4.55), 0.52, 0.38, 0.88, metal, root, 20)
    sphere("Lamp Glow", (0, 0, 4.62), (0.36, 0.36, 0.48), glass, root, 24, 16)
    cone("Lamp Cap", (0, 0, 5.18), 0.58, 0.06, 0.42, metal, root, 20)
    return root


def build_planter():
    root = empty("Planter")
    terracotta = material("Terracotta", (0.67, 0.20, 0.08, 1.0), roughness=0.72)
    rim = material("TerracottaRim", (0.91, 0.36, 0.13, 1.0), roughness=0.66)
    soil = material("Soil", (0.12, 0.055, 0.025, 1.0), roughness=0.90)
    leaf = material("FlowerLeaves", PALETTE["green"], roughness=0.75)
    pink = material("FlowerPink", (1.0, 0.20, 0.47, 1.0), roughness=0.55)
    yellow = material("FlowerYellow", (1.0, 0.68, 0.08, 1.0), roughness=0.48)
    cone("Planter Pot", (0, 0, 0.48), 0.82, 1.06, 0.92, terracotta, root, 28)
    torus("Planter Rim", (0, 0, 0.91), 0.90, 0.13, rim, root)
    cylinder("Planter Soil", (0, 0, 0.94), 0.82, 0.08, soil, root, 28, bevel=0.02)
    for i in range(7):
        angle = i * math.tau / 7
        radius = 0.42 if i else 0.0
        x, y = math.cos(angle) * radius, math.sin(angle) * radius
        cylinder(f"Flower Stem {i}", (x, y, 1.28 + (i % 2) * 0.12), 0.035, 0.66, leaf, root, 10, bevel=0.0)
        sphere(f"Flower Petal {i}", (x, y, 1.64 + (i % 2) * 0.12), (0.25, 0.25, 0.16), pink if i % 2 else yellow, root, 16, 10)
        sphere(f"Flower Center {i}", (x, y - 0.17, 1.64 + (i % 2) * 0.12), (0.08, 0.05, 0.08), yellow, root, 12, 8)
    return root


def build_cafe_set():
    root = empty("CafeSet")
    wood = material("CafeWood", PALETTE["wood_light"], roughness=0.52)
    metal = material("CafeMetal", PALETTE["metal"], metallic=0.40, roughness=0.26)
    fabric = material("UmbrellaFabric", (0.95, 0.19, 0.14, 1.0), roughness=0.62)
    fabric_light = material("UmbrellaStripe", (1.0, 0.76, 0.30, 1.0), roughness=0.62)
    cylinder("Cafe Table Top", (0, 0, 1.15), 1.45, 0.18, wood, root, 32, bevel=0.09)
    cylinder("Cafe Table Stem", (0, 0, 0.58), 0.15, 1.10, metal, root, 18, bevel=0.04)
    cylinder("Cafe Table Foot", (0, 0, 0.12), 0.62, 0.20, metal, root, 24, bevel=0.06)
    cylinder("Cafe Umbrella Pole", (0, 0, 3.10), 0.085, 4.2, metal, root, 16, bevel=0.02)
    for i in range(12):
        angle = i * math.tau / 12
        cone(f"Umbrella Panel {i}", (0, 0, 5.05), 3.0, 0.15, 0.62, fabric if i % 2 else fabric_light, root, 3, (0, 0, angle))
    sphere("Umbrella Finial", (0, 0, 5.42), (0.16, 0.16, 0.20), metal, root)
    for i, angle in enumerate((0.0, math.pi)):
        x, y = math.cos(angle) * 2.05, math.sin(angle) * 2.05
        chair = empty(f"Cafe Chair {i}", (x, y, 0), root)
        rounded_cube(f"Cafe Chair Seat {i}", (0, 0, 0.76), (1.0, 0.9, 0.18), wood, 0.08, chair)
        rounded_cube(f"Cafe Chair Back {i}", (0, 0.40, 1.38), (1.0, 0.18, 1.08), wood, 0.08, chair)
        for j, lx in enumerate((-0.38, 0.38)):
            rounded_cube(f"Cafe Chair Leg {i} {j}", (lx, 0, 0.36), (0.12, 0.70, 0.72), metal, 0.04, chair)
    return root


def build_shop():
    root = empty("Shop")
    wall = material("ShopTint", (0.88, 0.30, 0.35, 1.0), roughness=0.66)
    trim = material("ShopTrim", PALETTE["stone_light"], roughness=0.62)
    roof = material("ShopRoof", (0.50, 0.10, 0.065, 1.0), roughness=0.64)
    roof_tile = material("ShopRoofTile", (0.77, 0.22, 0.10, 1.0), roughness=0.68)
    glass = material("ShopGlass", (0.08, 0.56, 0.76, 1.0), metallic=0.08, roughness=0.14)
    awning = material("ShopAwning", (1.0, 0.62, 0.12, 1.0), roughness=0.55)
    white = material("ShopAwningWhite", PALETTE["white"], roughness=0.58)
    green = material("ShopPlants", PALETTE["green_light"], roughness=0.72)
    flower = material("ShopFlowers", (1.0, 0.18, 0.42, 1.0), roughness=0.58)
    rounded_cube("ShopTint Building", (0, 0, 4.1), (10.8, 5.7, 8.2), wall, 0.34, root)
    rounded_cube("Shop Foundation", (0, -2.92, 0.34), (11.4, 0.40, 0.68), trim, 0.12, root)
    rounded_cube("Shop Roof", (0, 0, 8.42), (11.8, 6.5, 0.65), roof, 0.20, root)
    for i, y in enumerate((-2.45, -1.25, 0.0, 1.25, 2.45)):
        rounded_cube(f"Shop Roof Rib {i}", (0, y, 8.77), (11.45, 0.12, 0.09), roof_tile, 0.035, root)
    for side, x in (("L", -3.05), ("R", 3.05)):
        rounded_cube(f"Shop Window {side}", (x, -2.90, 3.65), (3.45, 0.15, 3.60), glass, 0.10, root)
        for fx in (-1.62, 1.62):
            rounded_cube(f"Shop Window Frame {side} {fx}", (x + fx * 0.96, -3.02, 3.65), (0.15, 0.18, 3.85), trim, 0.04, root)
        rounded_cube(f"Shop Window Cross {side}", (x, -3.02, 3.65), (3.60, 0.18, 0.15), trim, 0.04, root)
    rounded_cube("Shop Door", (0, -2.96, 2.12), (1.78, 0.20, 4.24), roof, 0.12, root)
    rounded_cube("Shop Door Glass", (0, -3.08, 2.38), (1.30, 0.08, 2.92), glass, 0.05, root)
    sphere("Shop Door Knob", (0.58, -3.18, 2.10), (0.10, 0.06, 0.10), awning, root)
    rounded_cube("Shop Awning", (0, -3.68, 5.95), (10.3, 1.56, 0.32), awning, 0.10, root, (0.16, 0, 0))
    for i, x in enumerate((-4.2, -2.1, 0, 2.1, 4.2)):
        rounded_cube(f"Shop Awning Stripe {i}", (x, -3.71, 5.97), (0.86, 1.62, 0.34), white, 0.06, root, (0.16, 0, 0))
    rounded_cube("Shop Sign", (0, -3.05, 7.28), (4.6, 0.22, 0.92), roof, 0.16, root)
    rounded_cube("Shop Sign Inset", (0, -3.18, 7.28), (3.98, 0.06, 0.48), awning, 0.08, root)
    for side, x in (("L", -3.05), ("R", 3.05)):
        rounded_cube(f"Shop Flower Box {side}", (x, -3.25, 1.66), (3.35, 0.72, 0.48), roof, 0.12, root)
        for i in range(6):
            px = x - 1.25 + i * 0.5
            sphere(f"Shop Plant {side} {i}", (px, -3.50, 2.03), (0.28, 0.24, 0.34), green, root, 16, 10)
            sphere(f"Shop Flower {side} {i}", (px, -3.72, 2.12), (0.10, 0.08, 0.10), flower if i % 2 else awning, root, 12, 8)
    return root


ASSETS = [
    ("HibiAvatar.fbx", build_avatar),
    ("Fountain.fbx", build_fountain),
    ("Tree.fbx", build_tree),
    ("Bench.fbx", build_bench),
    ("Lamp.fbx", build_lamp),
    ("Planter.fbx", build_planter),
    ("CafeSet.fbx", build_cafe_set),
    ("Shop.fbx", build_shop),
]


def build_preview():
    reset_scene()
    imported_groups = []
    placements = {
        "HibiAvatar.fbx": (-4.0, 0.0, 0.0),
        "Fountain.fbx": (1.0, 2.2, 0.0),
        "Tree.fbx": (6.0, 2.2, 0.0),
        "Bench.fbx": (4.4, -2.0, 0.0),
        "Lamp.fbx": (-0.2, -2.1, 0.0),
        "Planter.fbx": (-6.0, -1.6, 0.0),
    }
    for filename, _ in ASSETS:
        if filename not in placements:
            continue
        before = set(bpy.context.scene.objects)
        bpy.ops.import_scene.fbx(filepath=str(OUTPUT_DIR / filename))
        imported = [obj for obj in bpy.context.scene.objects if obj not in before]
        offset = placements[filename]
        group = empty("Preview " + filename)
        for obj in imported:
            if obj.parent is None:
                obj.parent = group
            if filename == "HibiAvatar.fbx" and obj.name.startswith(("HairStyle_1", "HairStyle_2", "HairStyle_3")):
                obj.hide_render = True
                for child in obj.children_recursive:
                    child.hide_render = True
        group.location = offset
        imported_groups.append(group)

    floor_mat = material("PreviewFloor", (0.58, 0.76, 0.57, 1.0), roughness=0.82)
    rounded_cube("Preview Floor", (0, 1.0, -0.24), (18.0, 12.0, 0.45), floor_mat, 0.20)
    bpy.ops.object.light_add(type="AREA", location=(-4.0, -6.0, 11.0))
    key = bpy.context.object
    key.data.energy = 1500
    key.data.shape = "DISK"
    key.data.size = 7.0
    key.rotation_euler = (math.radians(25), 0, math.radians(-22))
    bpy.ops.object.light_add(type="AREA", location=(7.0, -1.0, 7.0))
    fill = bpy.context.object
    fill.data.energy = 900
    fill.data.color = (0.48, 0.70, 1.0)
    fill.data.size = 6.0
    bpy.ops.object.camera_add(location=(13.5, -20.0, 11.0), rotation=(math.radians(67), 0, math.radians(35)))
    camera = bpy.context.object
    bpy.context.scene.camera = camera
    constraint = camera.constraints.new(type="TRACK_TO")
    target = empty("Camera Target", (0.0, 1.0, 2.5))
    constraint.target = target
    constraint.track_axis = "TRACK_NEGATIVE_Z"
    constraint.up_axis = "UP_Y"
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1280
    scene.render.resolution_y = 720
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = str(OUTPUT_DIR / "hibi-assets-preview.png")
    scene.render.film_transparent = False
    scene.world.color = (0.20, 0.34, 0.48)
    scene.view_settings.look = "AgX - Medium High Contrast"
    bpy.ops.wm.save_as_mainfile(filepath=str(OUTPUT_DIR / "HibiAssetLibrary.blend"))
    bpy.ops.render.render(write_still=True)


for filename, builder in ASSETS:
    reset_scene()
    builder()
    export_fbx(filename)
    print(f"HIBI_ASSET_EXPORTED {filename}")

build_preview()
print(f"HIBI_ASSET_LIBRARY_OK {OUTPUT_DIR}")
