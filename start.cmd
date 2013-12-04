set user=Antonios
set pw=1234
:: header username password ASE_Name SpreadName BS QTY Price L1Market L1Product L1Type L1Contract L1Customer L1Ratio L1Multiplier L1ActiveQuote(T/F)  L2Market L2Product L2Type L2Contract L2Customer L2Ratio L2Multiplier L2ActiveQuote(T/F)
start ASEOrderRoutingSample.exe %user% %pw% AlgoSE FromCMDFile B 9 120 CME CL FUTURE MAR14 TTAPI 1 1 T CME CL FUTURE JUN14 TTAPI -1 -1 T