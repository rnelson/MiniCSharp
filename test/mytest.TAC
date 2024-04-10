PROC firstclass.firstclass2
  WRS _S0
  RDI _BP+8
  _BP-2 = -_BP+8
  _BP+8 = _BP-2
  WRS _S1
  WRI _BP+8
  WRLN
  i = 4
  j = 2
  WRS _S2
  WRI i
  WRI j
ENDP firstclass.firstclass2
PROC someclass.Main
  PUSH _BP-4
  PUSH _BP-2
  CALL firstclass.firstclass2
ENDP someclass.Main
START someclass.Main