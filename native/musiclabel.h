#ifndef MUSICLABEL_H
#define MUSICLABEL_H

#include <stdint.h>

#if defined(_WIN32)
    #define ML_EXPORT __declspec(dllexport)
#else
    #define ML_EXPORT __attribute__((__visibility__("default")))
#endif

#ifdef __cplusplus
extern "C" {
#endif
ML_EXPORT int64_t ml_service_fee_cents(int64_t amount_cents, int32_t fee_bps);

#ifdef __cplusplus
}

#endif
#endif