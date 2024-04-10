PROC firstclass.firstclass
ENDP firstclass.firstclass
PROC firstclass.secondclass
  _BP-10 = _BP-6 * _BP-8
  _BP-12 = _BP-2 + _BP-10
  _BP-4 = _BP-12
    _AX = _BP-4
ENDP firstclass.secondclass
PROC someclass.Main

  CALL firstclass.secondclass
  _BP-2 = _AX
  WRS _S0
  WRI _BP-2
  WRLN
ENDP someclass.Main
START someclass.Main