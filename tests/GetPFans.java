
import java.io.*;
import java.net.*;
import java.util.*;
import DigestSplitter.*;

/**
 * <strong>GetPFans</strong> -- making a URL connection and
 * getting information from it.
 */
public class GetPFans 
{ 

	/** Perform the process of binding to a URL
	 * @param urlString URL to bind to.
	 * @return Returns an input stream for the URL.
	 */
	static InputStream BindToURL(String urlString)
	{
		URL url;
		URLConnection connection;
		int contentLength;
		String contentType;
		InputStream inStream;
		
		/* Create URL, make the connection, and get its input stream.
		 * NOTE:  methods for getting URLConnection information, such as
		 * getContentLength() and getContentType(), require a prior call to
		 * getInputStream(), even if you don't actually read from the stream.
		 */
		try 
		{
		    url = new URL(urlString);
		    connection = url.openConnection();
		    inStream = connection.getInputStream();
		} catch(MalformedURLException e) 
		{
		    System.err.println("Exception:  " + e);
		    return null;
		} catch(IOException e) 
		{
		    System.err.println("Exception:  " + e);
		    return null;
		}

		
		return inStream;
	}
	
    public static void main(String[] args)
	{
		InputStream inStream;
		String list[] = {"911",
						"General", 
						"356",
						"914",
						"924-944",
						"928",
						"Boxster",
						"Racing",
						"Flamers",
						 "Tavern"};
		String month[] = {"01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "12"};
		String day[] = {"01", "02", "03", "04", "05", "06", "07", "08", "09", "10", 
						"11", "12", "13", "14", "15", "16", "17", "18", "19", "20", 
						"21", "22", "23", "24", "25", "26", "27", "28", "29", "30", "31"};
		String baseURL = "http://www.porschefans.com/Scripts/PorscheFansArchive.pl?PorscheFansArchive=";
		
		String outRoot = new String(".");
		
		Calendar rightNow = Calendar.getInstance();

		int startYear = 96;
		int endYear = rightNow.get(rightNow.YEAR) - 1900;
		int startDay = 1;
		int endDay = rightNow.get(rightNow.DAY_OF_MONTH);
		int startMonth = 1;
		int endMonth = rightNow.get(rightNow.MONTH)+1;
		
		
		for (int a = 0 ; a < args.length ; a++)
		{
			StringTokenizer st = new StringTokenizer(args[a], ":");
			String cmd;
			while (st.hasMoreTokens()) 
			{         
				cmd = st.nextToken();
				if (cmd.compareTo("/h") == 0)
				{
					System.err.println("Usage: GetPFans [/h] [/s:yy-mm-dd] [/d:dest-dir]");
					System.err.println("  Where");
					System.err.println("     /h = Help.");
					System.err.println("     /s = Specify start date (in the form yy[-mm[-dd]])");
					System.err.println("     dest-dir = Desitination directory");
					System.exit(1);
					continue;
				}
				if (cmd.compareTo("/s") == 0)
				{
					StringTokenizer st2 = new StringTokenizer(st.nextToken(), "-");
					Integer i = new Integer(st2.nextToken());
					startYear = i.intValue();
					if (st2.hasMoreTokens())
					{
						i = new Integer(st2.nextToken());
						startMonth = i.intValue();
						if (st2.hasMoreTokens())
						{
							i = new Integer(st2.nextToken());
							startDay = i.intValue();
						}
					}
					continue;
				}
				if (cmd.compareTo("/d") == 0)
				{
					outRoot = st.nextToken();
					continue;
				}
			}			
		}
		
		System.out.println("Getting PFans Archives starting at " + month[startMonth-1] + "/" + day[startDay-1] + "/" + startYear);
		
		try
		{
			String url;
			String sOutDir;

			int startMonthTemp = startMonth;
			int startDayTemp = startDay;
			int startYearTemp = startYear;

			// For each of the 10 lists
			for (int i = 0; i < list.length ; i++)
			{
				// Create directory
				if (args.length >= 1)
				{
					sOutDir = outRoot + File.separator + list[i];
				}
				else
					sOutDir = list[i];
				System.out.println("Creating directory: " + sOutDir);
				//File dir = new File(sOutDir);
				//dir.mkdirs();

				startYear = startYearTemp;
				startMonth = startMonthTemp;
				startDay = startDayTemp;
				
				boolean fEnd=false;
				
				for (int y = startYear; y <= endYear ; y++)
				{
					for (int m = startMonth-1; m < 12; m++)
					{
						startMonth = 1; // only use real start value for first year.
						
						for (int d = startDay-1; d < 31; d++)
						{
							startDay = 1; // only use real start value for first month
							
							if (y == endYear && m == (endMonth-1) && d >= endDay)
							{
								fEnd = true;
								break;
							}
							
							System.out.println("List=" +list[i]  + ", Year=" + y + ", Month=" + month[m] + ", Day=" + day[d]);
							url =  baseURL + list[i] + "&Month=" + month[m] + "&Day=" + day[d] + "&Year=" + y;

							inStream = GetPFans.BindToURL(url);
			
							BufferedReader in = new BufferedReader(new InputStreamReader(inStream));
							String s = null;

							try 
							{
								// look for <pre> or 'no archive"
								s = in.readLine();
								while (s != null)
								{
									if (s.compareTo("<pre>") == 0) 
										break; // Time to go to work!
									if (s.compareTo("<h2>Sorry, there is no PorscheFans Archive available for this date.</h2>") == 0)
									{
										System.out.println("  No archive.");
										s = null; // abort!
										break;
									}
									s = in.readLine();
								}
								
								if (s == null)
								{
									System.out.println("  No file created.");
									in.close();
									inStream.close();
									continue; // skip this file
								}
								
								// Create a temporary file
								PrintWriter out = new PrintWriter(new BufferedWriter(new FileWriter("GetPFans~.tmp")));
								
//								out.println("PorscheFans " + list[i] + " list archive");
//								out.println("For " + month[m] + "/" + day[d] + "/" + y);
//								out.println("---------------------");

								// Read/Write to temp file
								s = in.readLine();
								while (s != null)
								{
									if (s.compareTo("</pre>") == 0) // we are done
										break;
									out.println(s);
									s = in.readLine();
								}
								out.close();
								in.close();
								inStream.close();
								
								String sDigest = y + month[m] + day[d] + "-";
								// Append year to output directory
								String sTmp = new String(sDigest.substring(sDigest.length() - 7, sDigest.length()-5));
								if (Integer.parseInt(sTmp) < 50) 
									sTmp = "20" + sTmp;
								else
									sTmp = "19" + sTmp;
								sOutDir = sOutDir + File.separator + sTmp;

								// Append month to output directory
								sTmp = new String(sDigest.substring(sDigest.length() - 5, sDigest.length()-3));
								sOutDir = sOutDir + File.separator + sTmp;

								// Create the directory
								File f = new File(sOutDir);
								f.mkdirs();
								
								System.out.print("  Splitting " + sDigest + " to " + sOutDir + "...");
								inStream = new FileInputStream("GetPfans~.tmp");
								DigestSplitter splitter = new DigestSplitter(inStream, sOutDir, sDigest);
								splitter.Process();
								inStream.close();
								
								System.out.println(".done!");
							} catch (IOException e)	
							{
							    System.err.println("Exception reading:  " + e);
								return;
							}
						} // day
						if (fEnd == true)
							break;
					} // month
					if (fEnd == true)
						break;
				} // year
			} // list
		} catch (Exception e)	
		{
		    System.err.println("Exception:  " + e);
			return;
		}
	}
}
