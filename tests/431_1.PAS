Program SIE431_ass1;
{
  Copyright (C) 1989 Charles E. Kindel, All Rights Reserved.

  Program to read in an unknown amount of data, and calculate the
  sample mean, sample variance, and a 95% confidence interval on the
  data.

  Program should run under Turbo Pascal 5.0.
}

  var n : integer ;
      x, sum, sumsq, ci, average, variance : real ;


begin
  n := 0 ;
  sum := 0 ;
  sumsq := 0 ;
  x := 0 ;
  while not eof do
    begin
      readln (x) ;
      sum := sum + x ;
      sumsq := sumsq + (x*x) ;
      inc(n)
    end ;
  average := sum / n ;
  variance := 1 / (n-1)*(sumsq - (sum*sum/n)) ;
  ci := 1.645*sqrt(variance)/sqrt(n) ;
  writeln ('The average is: ', average:5:3) ;
  writeln ('The variance is: ', variance:5:3) ;
  writeln ('A 95% confidence interval is: ', average:5:3, ' +- ', ci:5:3) ;
  writeln ;
  writeln ('Done!')
end.


