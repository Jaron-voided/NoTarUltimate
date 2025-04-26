// See https://aka.ms/new-console-template for more information


using NoTarUltimate;

string input = "./TestFiles";
string notar = "./TestFilesPacked.notar";
NotarPackage notarPackage = new NotarPackage();
notarPackage.FromDirectory(input);
notarPackage.Pack(notar);