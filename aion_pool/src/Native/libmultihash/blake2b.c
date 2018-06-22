#include "blake2b.h"
#include <stdlib.h>
#include <stdint.h>
#include <string.h>
#include <stdio.h>

#include "sha3/sph_blake2b.h"

void blake2b_hash(const char* input, char* output, int inlen)
{
    blake2b_ctx ctx_blake2b;
    blake2b_init(&ctx_blake2b, 32, NULL, 0);
    blake2b_update(&ctx_blake2b, input, inlen);
    blake2b_final(&ctx_blake2b, output);
}
