// Stub Sentry GDExtension for Android. Returns failure so the engine skips loading it.

typedef int GDExtensionBool;
typedef void* GDExtensionInterfaceGetProcAddress;
typedef void* GDExtensionClassLibraryPtr;
typedef void* GDExtensionInitialization;

static const GDExtensionBool GDEXTENSION_INIT_FAILED = 0;

GDExtensionBool gdextension_init(
    GDExtensionInterfaceGetProcAddress p_get_proc_address,
    GDExtensionClassLibraryPtr p_library,
    GDExtensionInitialization *r_initialization
) {
    return GDEXTENSION_INIT_FAILED;
}
