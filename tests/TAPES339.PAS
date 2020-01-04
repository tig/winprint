Program Tapes; {ver 3.39}

{========================================================================}
{               TAPES - (c) 1986-1990 Charles E. Kindel, Jr.             }
{                     KindlCo Software Systems                           }
{                                                                        }
{ Written by Charles E. Kindel                                           }
{            Kindlco Software Systems                                    }
{            4225 N. First Ave #1315                                     }
{            Tucson, AZ 85719                                            }
{                                                                        }
{ Tapes is copyrighted by Charles E. Kindel, Jr. (1987,88,89,90). The Source}
{ code may is not be distributed, copied, or altered in any form.  The   }
{ executable code (TAPES.EXE) can be copied and distributed freely.      }
{ This program may not be sold, rented, or offered as                    }
{ as an incitement to buy any other product.                             }
{                                                                        }
{ Revisons:                                                              }
{                                                                        }
{       1/29/88                                                          }
{       V3.22 - Made RETURN go to the beginning of next line, as apposed }
{               to going to same position in next line.                  }
{               Fixed bug that would prompt to clear and exit even if    }
{               no data had been entered.                                }
{               Fixed print routines so the printer gets "reset" to      }
{               uncompressed and 1/6 inch line spacing.                  }
{                                                                        }
{       2/22/88                                                          }
{       V3.30 - Converted to Turbo Pascal 4.0                            }
{                                                                        }
{       5/28/88                                                          }
{       V3.31 - Added code to detect 43 line EGA or 50 line VGA.  TAPES  }
{               now switches out of these modes into 25 line mode when   }
{               it starts and goes back to original mode when it is done.}
{             - The cursor now changes size when Insert/Overwrite is     }
{               toggled with the Insert key.  Small cursor means insert  }
{               mode is on, block cursor means overwrite is on.          }
{             - Made the prompts for Clear, and Quit open windows.       }
{       7/18/88                                                          }
{       V3.32 - Enhanced ability to detect screen mode.                  }
{       10/5/88                                                          }
{       V3.33 - Recompiled in Turbo Pascal 5.0                           }
{                                                                        }
{       V3.34 - Fixed PgUp and PgDown so they worked more intuitivley    }
{                                                                        }
{       2/1/89                                                           }
{       V3.35 - Added option to print Tape Type and Noise Reduction      }
{               And added a menu window for print,clear,etc...           }
{                                                                        }
{       2/27/89                                                          }
{       V3.36 - Now can save default configuration of noise reduction    }
{               and tape type.  Searches current dir, path, and          }
{               dir where program was executed.  Added option to change  }
{               lpt port, user can now select between lpt1: lpt2:, com1:,}
{               and com2:.  This can be saved to the .EXE file.  Now     }
{               supports multiple printers.                              }
{                                                                        }
{       5/15/89                                                          }
{       V3.37 - Added tab key support.  Tab key now inserts 5 spaces     }
{                                                                        }
{       11/30/89                                                          }
{       V3.38 - Fixed bug in clear where after clear the cursor would    }
{               return to field 1.  Fixed keyboard handling problem in   }
{               Menu.  Previously if the down arrrow was pressed in Menu }
{               Print would happen.  Fixed copyright notices.            }
{                                                                        }
{       4/27/90                                                          }
{       V3.39 - Updated address and fixed color problems                 }
{                                                                        }
{========================================================================}


Uses
  Dos,
  TpString,
  TpDOS,
  TpCrt,
  TpWindow,
  sDefs;

   type PtrRec = record
           Comp_On : string [9];
           Comp_Off : string [9];
           Bold_On  : string [9];
           Bold_Off : string [9];
           One8th   : string [9];
           One6th   : string [9];
        end;
        fieldtype = string [50];

   Const
      idHeader:   String[8]  = 'XmtSpot ';
      biastype     : byte = 2;
      noisetype    : byte = 3;
      lpttype  : byte = 1;
      prntype  : byte = 1;

      Printers : array [1..2] of PtrRec =
               (    (Comp_On  : ^O ;         {Compressed on: change to suit your printer}
                    Comp_Off : ^R ;         {Compressed off: ditto.}
        {IBM/EPSON} Bold_On  : #27+'G';         {Turns on double strike}
                    Bold_Off : #27+'H';         {Turns double strike off}
                    One8th   : #27+'0';
                    One6th   : #27+ 'A'+ #12+ #27+ '2'
                    ),
                    (Comp_On : #27+'(s16.66H';         {Compressed on: change to suit your printer}
                    Comp_Off : #27+'(s10H';         {Compressed off: ditto.}
        {HP Laser}  Bold_On  : #27+'(s3B';         {Turns on double strike}
                    Bold_Off : #27+'(s0B';         {Turns double strike off}
                    One8th   : #27+'&l8D';
                    One6th   : #27+'&l6D'
                    )
               );
      LastByte : byte = 0;
      PrnName : array [1..2] of string [12] =
                ('IBM/Epson', 'HP Laser');




      Version = 3.39;
      Year = 1990;
   {Printer Codes:}
      Tab = ^I;
      Esc = ^[;
   {Graphics Characters:}
      Horiz = #196;          Vert  = #186;
      Right_T = #180;        Left_T = #195;
      Top_T = #210;          Bot_T = #208;
      Cross = #215;
   {Function Keys:}
      F10 = 323;
      Up = 327;              Down = 335;
      PgUp = 328;            PgDn = 336;
      CR  = 13;              BOTTOM = 336;
      DEL = 338;             DELEOL = 372;
      DELFLD = 374;          BKSPC = 8;
      HOME = 326;            ENDD= 334;
      LEFT = 330;            RIGHT = 332;
      INS  = 337;            TOP = 328;
      ESCKey = 27;           TABkey = 9 ;
      Month_ : array [1..12] of string [10] = ('January', 'February', 'March',
                                               'April', 'May', 'June', 'July',
                                               'August', 'September', 'October',
                                               'November', 'December');
      DayOfWeek_ : array [0..6] of string [10] = ('Sunday', 'Monday', 'Tuesday',
                                                  'Wednesday', 'Thursday', 'Friday',
                                                  'Saturday');
      Bias : array [1..3] of string [30] = ('Type I  (Normal Bias 120us)', 'Type II (CrO2 High Bias 70us)',
                                            'Type IV (Metal)');
      Noise : array [1..4] of string [10] = ('None', 'Dolby B', 'Dolby C', 'dbx');
      Lptname : array [1..4] of string [4] = ('Lpt1', 'Lpt2', 'com1', 'com2');

   Var
      Song_Names  : array [1..40] of fieldtype;
      FieldNum,               {Field number on the screen}
      Pos,                    {Position of cursor on line}
      Htab,
      Vtab        : byte;
      i           : word;
      New_Page,               {Do we need to send a form feed?}
      Empty,                  {Is the data cleared?}
      InsertMode,
      Line43      : boolean;
      Key         : char;
      ptrwin, UtilWin     : WindowPtr;
      Status      : word;  {any modifications?}
      QuitFlag    : boolean;
      vbSize      : Word;     {Size of CONST block of default variables.}
      lst         : text;
      winMin, winMax  : word;


{$I Ding.pro}                       {Produces a small beep noise}
{$I Frame.pro}                      {Draws double lined frames}
{$I kindlco.pas}

  procedure SaveWindow;
    begin
      WinMin := WindMin;
      WinMax := WindMax;
      window (1,1,80,25);
    end;

  procedure RestoreWindow;
    begin
      window (Lo(WinMin)+1,Hi(WinMin)+1, Lo(WinMax)+1, Hi(WinMax)+1);
    end;

  procedure ClearStatLine;
    begin
      HiddenCursor;
      Window (1,25,80,25);
      ClrScr;
      Window (1,1,80,25);
    end;

  Procedure ErExit;
    Begin
    savewindow;
    GoToXY(1,25);
    TextColor (Yellow);
    Write(center (ErSt,79)); {The error report string is provided by sDefs.TPU}
    ding;
  end;

  Procedure SaveConfig;
    var FileName : string;
        newbias, newnoise,newlpt,newprn : byte;

    Begin
      if (Hi (DosVersion) < 3) then
        begin
          if not (ExistOnPath ('TAPES.EXE', filename)) then
            begin
              SaveWindow;
              TextColor (Yellow);
              GotoXY (1,25);
              writeln (center('Cannot Find TAPES.EXE; If you are using a version of DOS less than 3.0',79));
              write (center('then TAPES.EXE must be in current directory or in the PATH.',79));
              ding;
              RestoreWindow;
              exit;
            end;
        end
      else
        filename := paramstr (0);
      newbias := biastype;
      newnoise := noisetype;
      newlpt := lpttype;
      newprn := prntype;
      vbSize:= Ofs(LastByte)-Ofs(idheader); {Segments will be equal.}
      SaveWindow;
      TextColor (Yellow);
      GotoXY (1, 25);
      Write(Center('Updating '+ filename+ ' with new configuration information...',79));
      If NOT PrgFileLoaded(filename) then
        begin
          erexit;
          exit;
        end;
      If NOT DefRecFound(@idHeader, SizeOf(idHeader) ) then
        begin
          ErExit;
          exit;
        end;
      ReadMemBlk(@idheader, vbSize);
      biastype := newbias;
      noisetype := newnoise;
      lpttype := newlpt;
      prntype := newprn;
      WriteMemBlk(@idHeader, vbSize);
      If NOT PrgFileSaved(filename) then
        begin
          ErExit;
          exit;
        end;
      GotoXY (1,25) ;
      ClrEol ;
      RestoreWindow;
   end;


  Procedure TitlePage;
      var VerS : string [5];

      begin
        ClrScr;
        TextColor (MapColor (Yellow));
        str (Version:5:2, VerS);
        writeln;
        Writeln (Center ('נננ T A P E S - V'+VerS+' נננ',currentwidth-1));
        writeln;
        writeln;
        TextColor (MapColor (LightRed));
        kindlco;
        writeln;
        TextColor (MapColor (White));
        writeln (Center ('This program is FreeWare',currentwidth-1));
        writeln (Center ('If you find it useful please send a donation ($5) to',currentwidth-1));
        writeln;
        writeln (Center ('Charles E. Kindel, Kindlco Software Systems',currentwidth-1));
        writeln (Center ('4225 North First Ave, #1315',currentwidth-1));
        writeln (Center ('Tucson, AZ  85719',currentwidth-1));
      end;

  Procedure PrintInsertMode;
    begin
      GotoXY (71, 25);
      TextBackground (MapColor (Red)); TextColor (black);
      if InsertMode then
        begin
          write (' Insert  ');
          NormalCursor;
        end
      else
        begin
          write ('Overwrite');
          BlockCursor;
        end;
      TextBackground (Black); TextColor (white);
    end;

  Procedure StatLine;
    begin
      ClearstatLine;
      TextColor (MapCOlor(LightCyan));
      GotoXY (1,25);
      write (Center ('Press [ESC] for menu or [F10] to quit',79));
      PrintInsertMode;
    end;

  Procedure DataOut;
    var Side, Field, Song : byte;

    begin
      For Side := 0 to 1 do
        begin
          case Side of
            0 : begin
                  Window (2, 4, 39,23);
                  ClrScr;
                  GotoXY (1, 1); write (Song_Names[1]);
                end;
            1 : begin
                  Window (42, 4, 79, 23);
                  ClrScr;
                  GotoXY (1, 1); write (Song_Names[20]);
                end;
          end;
          GotoXY (1,2);
          For i := 1 to 38 do Write (Horiz);
          For Song := 2 to 19 do
            begin
              Field := Song + (Side*19);
              GotoXY (1, 1+Song);
              write ('  ', Song_Names[Field])
            end;
        end;
        Window (1, 1, CurrentWidth, CurrentHeight+1);
    end;

  Procedure ScreenOut;
  {outputs the data and the field numbers to the screen}

    Var   Row,
          Line,
          Htab,
          Vtab,
          Field
          : byte;
          temp
          : string ;

  Begin
      window (1, 1, CurrentWidth, CurrentHeight+1);
      ClrScr;
      TextColor (MapColor (14)); TextBackground (MapColor (black));
      Frame (1, 3, 40, 24);
      Frame (41, 3, 80, 24);
      GotoXY (1, 5); Write (Left_T);
      GotoXY (40, 5); Write (Right_T, Left_T);
      GotoXY (80, 5); Write (Right_T);
      TextColor (MapColor (white)); TextBackground (black);
      GotoXY (1,1);
      Str(Version:4:2,Temp) ;
      ClrEol ;
      write (Center ('TAPES V'+Temp+'   Kindlco Software Systems',79));
      GotoXY (1,2) ;
      ClrEol ;
      Write (Center ('Copyright (C) 1986-'+Long2Str(Year)+' Charles E. Kindel, Jr.',79));
      TextColor (MapColor (LightGreen)) ;
      GotoXY (15, 3); write (' Side A ');
      GotoXY (55, 3); write (' Side B ');
      StatLine;
    end;

  Procedure CleanUp;
    var
       i
       : byte;

    begin
      for i := 1 to 38 do
        Song_Names [i] := '';
      Empty := true;
      FieldNum := 1;
      Pos := 1;
    end;

  Procedure Clear;
    var  Key
         : char;

    begin
      if not empty then
        begin
          ClearStatLine;
          Ding;
          if not MakeWindow (UtilWin, 22, 8, 60, 13, True, True, False,
                             White, Yellow, LightRed, ' Clear ')
          then
            writeln ('ERROR: MakeWindow')
          else
            if not DisplayWindow (UtilWin) then
              writeln ('ERROR: DisplayWindow')
            else
              begin
                Window (22,8,60,13);
                Key := 'N';
                GotoXY (2,3);
                Write (Center ('Are you sure you want to clear',36));
                GotoXY (2,4);
                write (Center('the data [y/N]?',36));
                Key := ReadKey;
                UtilWin := EraseTopWindow;
                DisposeWindow (UtilWin);
                Window (1,1,CurrentWidth,CurrentHeight+1);
                if (UpCase (Key) = 'Y') then
                  begin
                    Cleanup;
                    DataOut;
                  end;
              end;
          StatLine;
        end;
     end;



  Procedure Quit;
    var  Key
         : char;

    begin
      if not empty then
        begin
          ClearStatline;
          Ding;
          if not MakeWindow (UtilWin, 22, 8, 60, 13, True, True, False,
                             White, LightBlue, LightGreen, ' Quit ')
          then
            writeln ('ERROR: MakeWindow')
          else
            if not DisplayWindow (UtilWin) then
              writeln ('ERROR: DisplayWindow')
            else
              begin
                Window (22,8,60,13);
                Key := 'N';
                GotoXY (2,3);
                write (center ('Are you sure you want to quit',36));
                GotoXY (2,4);
                write (Center('[y/N]?',36));
                Key := ReadKey;
                if (UpCase (Key) <> 'Y') then
                  QuitFlag := False
                else
                  QuitFlag := True;
                UtilWin := EraseTopWindow;
                DisposeWindow (UtilWin);
              end;
          Window (1,1,CurrentWidth,CurrentHeight+1);
          StatLine;
        end
      else
        QuitFlag := True;
    end;


  Procedure Print;
    Var  i,
         Field : byte;
         Yr, Mo, Day, DayOfWeek :word;
         DateStr :string [30];

     Procedure PrintField;
       begin
         if Field = 1 then
           begin
             write (Lst, Printers[prntype].Bold_On);
             write (Lst, '| ', copy (Song_Names[1], 1, 32));
             write (Lst, ' ':(34 - length (copy (Song_Names[1], 1, 32))));
             write (Lst, copy(Song_Names[20], 1, 32));
             if (length (copy (Song_Names[20], 1, 32)) <> 32) then
               write (Lst, ' ':(32 - length (copy(Song_Names[20], 1, 32))));
             writeln (Lst, '|', Printers[PrnType].Bold_Off);
           end
         else
           begin
             write (Lst, '| ', Song_Names[Field]);
             write (Lst, ' ':(34 - length (copy (Song_Names[Field], 1, 31))));
             write (Lst, Song_Names[Field + 19]);
             write (Lst, ' ':(32 - length (copy(Song_Names[Field+19], 1, 31))));
             writeln (Lst, '|');
           end;
      end; {PrintField}

     Procedure PrintBlank;
       begin
         writeln (Lst, '|', ' ':67, '|');
       end;

     Procedure PrintLarge;
       begin
         write (Lst, Printers[PrnType].Bold_On,Printers[PrnType].One6th);
         writeln (Lst, '  ', Song_Names [1]);
         write (Lst, '  ', Song_Names [20]);
         writeln (Lst, Printers[PrnType].Bold_Off)
       end; {PrintLarge}

     begin  {Printer}
       assign (lst, lptname[lpttype]);
       rewrite (lst);
       writeln (Lst, Printers[PrnType].One8th , Printers[PrnType].Comp_On);
       for i := 1  to 69 do
         write (Lst, '-');
       writeln (Lst);
       for Field := 1 to 19 do
         begin
           PrintField;
           if Field = 1 then PrintBlank;
         end;
       Writeln (Lst, Printers[PrnType].Comp_Off);
       PrintLarge;
       writeln (Lst, Printers[PrnType].One8th, Printers[PrnType].Comp_On);
       PrintBlank;
       writeln (Lst, '| ', bias[biastype], ' ':66-Length(Bias[Biastype]),'|');
       writeln (Lst, '| Noise Reduction: ',Noise[Noisetype],' ':49-Length(Noise[Noisetype]),'|');
       GetDate (Yr, Mo, Day, DayofWeek);
       DateStr := DayOfWeek_[DayofWeek]+' '+Month_[mo]+' '+Long2Str(Day) +', '+Long2Str(Yr);
       writeln (Lst, '|', ' ':66-Length(DateStr), DateStr,' |');
       for i := 1 to 69 do write (Lst, '-');
       for i := 1 to 3 do writeln (Lst);
       if New_Page then writeln (Lst, chr(12));
       writeln (Lst, Printers[PrnType].One6th, Printers[PrnType].Comp_Off);
       close (lst);
     end;




  Procedure Menu;
    const
       MenuItem : array [1..8] of string [30] = ('Print a Cassette Insert.', 'Clear Data Now In Memory.', 'Tape Type:',
                                            'Noise Reduction:', 'Lpt Port:',
                                            'Install Printer:','Save Default Configuration.',
                                            'Quit TAPES and exit to Dos.');
    var
       i : byte;
       Dummy : char ;

        Procedure BiasPrint;
          var i:byte;
          begin
            GotoXY (22,3);
            for i := 1 to 3 do
              begin
                if biastype = i then
                  TextColor (Yellow)
                else
                  TextColor (LightGray);
                write (copy (bias[i],1,8),'   ');
             end;
          end;

        Procedure NoisePrint;
          var i:byte;
          begin
            GotoXY (22,4);
            for i := 1 to 4 do
              begin
                if noisetype = i then
                  TextColor (Yellow)
                else
                  TextColor (LightGray);
                write (noise[i],'   ');
              end;
          end;

        procedure lptprint;
          var i : byte;
          begin
            GotoXY (22,5);
            for i := 1 to 4 do
              begin
                if lpttype = i then
                  TextColor (Yellow)
                else
                  TextColor (LightGray);
                write (lptname[i],'   ');
              end;
          end;

        procedure prnprint;
          var i: integer;

          begin
            GotoXY (22, 6);
            for i := 1 to 2 do
              begin
                if prntype = i then
                  textcolor (yellow)
                else
                  textcolor (lightgray);
                write (prnname[i], '   ');
             end;
          end;



    begin
      ClearStatLine;
      if not MakeWindow (UtilWin, 12, 8, 68, 17, True, True, False,
                         black, White, LightBlue,
                         ' Press [ESC] to Exit Menu ') then
          writeln ('ERROR: MakeWindow')
      else
        if not DisplayWindow (utilWin) then
          writeln ('ERROR: DisplayWindow')
        else
          begin
            Window (13,9,67,16);
            ClrScr;
            for i := 1 to 8 do
              begin
                TextColor (LightRed);
                GotoXY (2,i);
                write (MenuItem[i,1]);
                TextColor (Green);
                write (Copy (MenuItem[i],2,Length(MenuItem[i])-1));
              end;
              BiasPrint;
              NoisePrint;
              lptPrint;
              prnPrint;
              repeat
                key := readkey;
                if Key = #0 then
                  Dummy := ReadKey;
                case Upcase(Key) of
                  'P' : Begin
                          SaveWindow;
                          ClearStatLine;
                          GotoXY (1,25);
                          TextColor (Yellow);
                          write (Center (' Printing...',79));
                          Print;
                          GotoXY (1,25);
                          ClrEol;
                          RestoreWindow;
                          New_Page := Not New_Page;
                        end;
                  'T' : begin
                          case BiasType of
                            1 : BiasType := 2;
                            2 : BiasType := 3;
                            3 : BiasType := 1;
                          end;
                          biasprint;
                        end;
                  'N' : begin
                          case Noisetype of
                            1 : NoiseType := 2;
                            2 : NoiseType := 3;
                            3 : NoiseType := 4;
                            4 : NoiseType := 1
                          end;
                          NoisePrint;
                        end;
                  'L' : begin
                          case lpttype of
                            1 : lpttype := 2;
                            2 : lpttype := 3;
                            3 : lpttype := 4;
                            4 : lpttype := 1;
                          end;
                          lptprint;
                        end;
                  'I' : begin
                          case prntype of
                            1 : inc(prntype);
                            2 : prntype := 1;
                          end;
                          prnPrint;
                        end;
                  'S' : begin
                          SaveConfig;
                        end;
                end;
              until (UpCase (key) in [ESC,'C','Q']);
          end;
      UtilWin := EraseTopWindow;
      DisposeWindow (utilWin);
      Window (1,1,CurrentWidth,CurrentHeight+1);
      case UpCase(key) of
        'C' : Clear;
        'Q' : Quit;
      end;
      StatLine;
    end;



   Function GetKeyCode : word;
     var code : word;
         key : char;

     begin
       code := 0;
       Key := ReadKey;
       if Key = #0 then
          begin
            Key := ReadKey;
            code := 255;
          end;
       GetKeyCode := code + ord (key);
     end;


   Function GetField (    X, Y,                  {Screen Coords}
                          MaxLen   : byte;    {Maximum length of field}
                      var Pos      : byte;    {Cursor position in field}
                      var Field    : fieldtype   {string to hold field}
                     ) : word;
    Var    Len:byte;
           KeyCode : word;   {current length of field}
           i: byte;

    Procedure DelC;
      begin
        if (Len > 0) and (Pos <= Len) then
          begin
            Delete (Field, Pos, 1);
            Write (Copy (Field, Pos, Len), ' ');
          end;
      end;

    Procedure RightC;
      begin
        if (Pos < MaxLen) then
          begin
            Pos := Pos + 1;
            if (Pos > Len) then Field := Concat (Field, ' ');
          end;
      end;

    Procedure LeftC;
      begin
        if (Pos > 1) then Pos := Pos -1;
      end;

    Procedure BackSpace;
      begin
        if (Pos > 1) then             {if we can backspace}
          begin
            if (Pos = MaxLen) and (Pos = Len) then
               Pos := Pos + 1;
            Pos := Pos - 1;
            Delete (Field, Pos, 1);   {Delete the character}
            GotoXY (X + (Pos-1), Y);
            Write (Copy (Field, Pos, Len), ' ');
          end;
     end;

    Procedure DelEndofLine;
      begin
        if (Len > 0) and (Pos <= Len) then
          begin
            Delete (Field, Pos, Len);
            write (' ':Len - Pos+1);
          end;
      end;

    Procedure DelField;
      begin
        Pos := 1;
        Field := ' ';
        GotoXY (X + (Pos-1), Y); write (' ':Len);
      end;

    Procedure PrintableChar;
      var c : char;
      begin
        c := chr (keycode);
        if (InsertMode) and (Len < MaxLen) then        {if the string is not too long}
           begin
             Insert (c, Field, Pos); {insert the character}
             Write (Copy (Field, Pos, Len+1));
             if (Pos < Maxlen) then Pos := Pos + 1;
           end
        else
          begin
            Delete (Field, Pos, 1);
            Insert (c, Field, Pos);
            if (Pos < MaxLen) then Pos := Pos + 1;
            write (c);
          end
      end;

    Procedure TabChar ;
      var i : byte;
      begin
        i := 0 ;
        if (InsertMode) then
          begin
            while ((Len < MaxLen) and (i < 5))  do
              begin
                Insert (' ', Field, Pos) ;
                Len := Length (Field) ;
                inc (i) ;
              end ;
            Write (Copy (Field, Pos, Len+1)) ;
            Pos := Pos+i ;
            if Pos > MaxLen then Pos := MaxLen;
          end
        else
          while (Len < MaxLen) and (i <=5) do
            begin
              Delete (Field, Pos, 1) ;
              Insert (' ', Field, Pos) ;
              if (Pos < MaxLen) then inc (Pos) ;
              write (' ') ;
            end ;
      end ;

    begin
      GotoXY (X, Y); Write (Field);
      repeat                                     {until Status <> 0}
         GotoXY (X+ (Pos-1), Y);
         Len := Length (Field);                  {How long is the string?}
         KeyCode := GetKeyCode;
         case KeyCode of
            DEL   : DelC;
            LEFT  : LeftC;
            RIGHT : RightC;
            TABKey: TabChar ;
            HOME  : Pos := 1;
            ENDD  : if (Len < MaxLen) then Pos := Len+1
                    else Pos := Len;
            DELEOL : DelEndofLine;
            DELFLD : DelField;
            INS    : begin
                       InsertMode := Not (InsertMode);
                       PrintInsertMode;
                     end;
            CR     : begin
                       Pos := 1;
                       KeyCode := DOWN;           {it was a RETURN}
                     end;
            BKSPC  : BackSpace;
            ESCkey : Menu;
            else   if (KeyCode < 256) then
                     begin
                       PrintableChar;
                       Empty := false;
                     end;
        end {case}
      until ((KeyCode > 255) or (QuitFlag) or (Empty));
      GetField := KeyCode;
    end;


  Procedure WhatField (Var Num_Chars, FieldNum : byte);
    begin
      If FieldNum > 19 then         {if field is on the right side (B)}
        begin
          Htab := 46; Vtab := FieldNum - 15;
        end
      else                       {or if it is on the left side (A)}
        begin
          Htab := 6; Vtab := FieldNum + 4
        end;
      if (FieldNum = 1) or (FieldNum = 20) then  {it is a Title}
        begin
          Num_Chars := 37;                  {it can be 37 chars long}
          Htab := Htab - 4;                {titles are longer...}
          Vtab := 4;
        end
      else                                 {it is a song}
        Num_Chars := 31;                    {it can be 31 chars long}
    end;



  Procedure FKeys;
    Var
         Num_Chars  : byte;
         EscCode    : char;

    begin
      repeat
        WhatField (Num_Chars, FieldNum);
        if (Pos > length (Song_Names [FieldNum])) then
          Pos := length (Song_Names [FieldNum]) + 1;
        Status := GetField (Htab, Vtab, Num_Chars, Pos, Song_Names [FieldNum]);
        Case Status of
          DOWN     : if (FieldNum = 38) then        {or CR}
                        FieldNum := 1
                     else
                        FieldNum := FieldNum + 1;
          Up       : if (FieldNum = 1) then
                        FieldNum := 38
                     else
                        FieldNum := FieldNum - 1;
          TOP      : begin
                       case FieldNum of
                         1  : FieldNum := 20;
                         20 : FieldNum := 1;
                       else
                         if FieldNum > 20 then
                           FieldNum := 20
                         else
                           FieldNum := 1;
                       end;
                       Pos := 1;
                     end;
          BOTTOM   : begin
                       case FieldNum of
                         1 : FieldNum := 20;
                         20 : FieldNum := 1;
                       else
                         if FieldNum > 20 then
                           FieldNum := 1
                         else
                           FieldNum := 20;
                       end;
                       Pos := 1;
                     end;
          F10      : Quit;
        end {case}
      until (QuitFlag);
    end;

{-------------------------  Main Program  ---------------------------------}

Begin
  window (1,1, CurrentWidth, CurrentHeight+1);
  if CurrentDisplay >= Ega then
    if Font8x8Selected then
      begin
        Line43 := True;
        SelectFont8x8 (False);
      end
    else
      Line43 := False;
  DirectVideo := True;
  New_Page := false;
  InsertMode := True;
  QuitFlag := False;
  CleanUp;
  TitlePage;
  i := 0;
  repeat
    i := i + 1;
  until ((KeyPressed) or (i > 60000));
  if KeyPressed then Key := ReadKey;
  ScreenOut;
  DataOut;
  FKeys;
  Window (1, 1, CurrentWidth, CurrentHeight+1);
  TitlePage;
  if Line43 then
    SelectFont8x8 (true);
  NormalCursor;
end.
