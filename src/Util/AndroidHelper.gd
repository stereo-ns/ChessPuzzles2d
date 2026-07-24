extends Node

func get_native_lib_dir() -> String:
    if OS.get_name() != "Android":
        return ""

    var android_runtime = Engine.get_singleton("AndroidRuntime")
    var activity = android_runtime.getActivity()
    var context = activity.getApplicationContext()
    var app_info = context.getApplicationInfo()

    var info_class = app_info.getClass()
    var field = info_class.getField("nativeLibraryDir")
    var raw_value = field.get(app_info)

    # raw_value je generički JavaObject omotač — pozivamo toString()
    # kao metodu da dobijemo pravi GDScript String
    var real_path: String = raw_value.call("toString")

    return real_path