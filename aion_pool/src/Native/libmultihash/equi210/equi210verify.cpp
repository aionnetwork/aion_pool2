#include <stdint.h>
#include "equi/equi210.h"
#include "equi210verify.h"

bool verifyEqui210(const char *hdr, const char *soln){
  unsigned int n = 210;
  unsigned int k = 9;

  bool isValid = verifyEH210(hdr, soln, n, k);

  return isValid;
}