#include <jni.h>
#include <dlfcn.h>

typedef void (*FmodAndroidInitFn)(JNIEnv *, jclass, jobject);
typedef void (*FmodAndroidCloseFn)(JNIEnv *, jclass);

static void *get_fmod_handle(void) {
    void *handle = dlopen("libfmod.so", RTLD_NOW | RTLD_GLOBAL);
    if (handle != 0) {
        return handle;
    }

    return RTLD_DEFAULT;
}

JNIEXPORT void JNICALL
Java_org_fmod_FMOD_nativeInit(JNIEnv *env, jclass clazz, jobject context) {
    void *handle = get_fmod_handle();
    FmodAndroidInitFn init = (FmodAndroidInitFn)dlsym(handle, "FMOD_Android_JNI_Init");
    if (init == 0) {
        jclass errorClass = (*env)->FindClass(env, "java/lang/UnsatisfiedLinkError");
        if (errorClass != 0) {
            (*env)->ThrowNew(env, errorClass, "FMOD_Android_JNI_Init was not found in libfmod.so");
        }
        return;
    }

    init(env, clazz, context);
}

JNIEXPORT void JNICALL
Java_org_fmod_FMOD_nativeClose(JNIEnv *env, jclass clazz) {
    void *handle = get_fmod_handle();
    FmodAndroidCloseFn close = (FmodAndroidCloseFn)dlsym(handle, "FMOD_Android_JNI_Close");
    if (close == 0) {
        return;
    }

    close(env, clazz);
}
