#include "musiclabel.h"

#define ML_MAX_AMOUNT_CENTS INT64_C(100000000000000)
#define ML_MAX_FEE_BPS      10000
#define ML_BPS_DIVISOR      10000
#define ML_HALF_DIVISOR     5000

ML_EXPORT int64_t ml_service_fee_cents(int64_t amount_cents, int32_t fee_bps)
{
    if (amount_cents < 0 || amount_cents > ML_MAX_AMOUNT_CENTS) return -1;
    if (fee_bps < 0 || fee_bps > ML_MAX_FEE_BPS) return -1;

    return (amount_cents * (int64_t)fee_bps + ML_HALF_DIVISOR) / ML_BPS_DIVISOR;
}