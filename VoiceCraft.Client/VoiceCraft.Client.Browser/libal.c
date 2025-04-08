// #include <AL/alc.h>
#include <emscripten.h>

struct ALCcontext_struct;
struct ALCdevice_struct;
typedef char ALCboolean;
typedef char ALCchar;
typedef double ALCdouble;
typedef float ALCfloat;
typedef int ALCenum;
typedef int ALCint;
typedef int ALCsizei;
typedef short ALCshort;
typedef signed char ALCbyte;
typedef struct ALCcontext_struct ALCcontext;
typedef struct ALCdevice_struct ALCdevice;
typedef unsigned char ALCubyte;
typedef unsigned int ALCuint;
typedef unsigned short ALCushort;
typedef void ALCvoid;
//
//
// EMSCRIPTEN_KEEPALIVE
// extern const ALCchar *alcGetString(ALCdevice *device, ALCenum param);
//
#ifdef __cplusplus
extern "C" {
#endif

EMSCRIPTEN_KEEPALIVE
__attribute__((visibility("default"))) const ALCchar *
alcGetString(ALCdevice *device, ALCenum param) {
  // const char *alcGetString(int device, int param) {
  // return _wrapper_alcGetString(device, param);
  return "test";
}

#ifdef __cplusplus
}
#endif
