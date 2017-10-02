# getQR
Command line tool to read QRCode using ZXing Library and Aforge.NET

# Syntax
```
Sintassi:
 extsp.exe [-emulate] [-debug] [-cropcenter] [-blob] [...] -f file_path 

Opzioni:
   -f file_path    Percorso dell'immagine su cui tentare la decodifica. Se  
                   viene indicato un file pdf questo viene convertito in    
                   immagine (con libreria ghostscript)
   -emulate        Solo simulazione per analisi delle prestazioni. Viene
                   restituito il tempo impiegato per la decodifica.
   -debug          Espone messaggi di debug circa l'iter di decodifica.
   -cropcenter     Ritaglia e analizza solo la parte centrale del documento.
                   Viene analizzato solo la parte di immagine compresa in un
                   rettangolo i cui estremi sono calcolati come segue:
                     x1 = larghezza documento * 0,3
                     y1 = altezza documento * 0,3
                     x2 = larghezza documento * 0,7
                     y2 = altezza documento * 0,7
   -blob           Evita di scansionare tutto il documento ed effettua una   
                   ricerca di possibili QRCode secondo parametri impostati. 
                   Vengono cercate e scansionate solo le regioni dell'immagi 
                   ne che corrispondono ai filtri impostati e che potrebbero
                   per caratteristiche geometriche contenere un QRCode.
                                                                            
   Parametri per la ricerca blob:                                           
     -bfilterw 0.15-0.30  Filtra solo blob con larghezza compresa tra il 10%
                          ed il 30% della grandezza totale dell'immagine.   
     -bfilterh 0.10-0.20  Filtra solo blob con altezza compresa tra il 10%  
                          ed il 20% della grandezza totale dell'immagine.   
     -bnoff               Non controllare il fattore di forma del blob. Per 
                          default si escludono fattori di forma non quadrati
                          ovvero con (larghezza/altezza)% < 95
     -bzone 0.5,0.6       La regione del blob (+-20%) per essere valida deve
                          contenere un punto definito alle coordinate x,y   
                          calcolate in percentuale rispetto alle dimensioni 
                          documento. Nell'esempio x = 50% larghezza doc. e  
                          y = 60% altezza documento. 
     -bnoshape            Non controllare la forma del blob. Per default ven
                          gono analizzati i bordi del blob per ricavarne  la 
                          forma dell'oggetto,c he deve risultare di tipo    
                          'Square'. Altre forme vengono ignorate, ma alcuni 
                          QRCode potrebbero comunque essere riconosciuti.   
     -bnotrotate          Non riallineare il QRCode prima di tentare la lett
                          ura. Per default, il blob viene riallineato prima 
                          di tentare la decodifica. L'angolo di rotazione è 
                          calcolato come Atan2(dx, dy) * (180 / Math.PI)    
                                                                            
     Esempi:                                                                
         getQR.exe -f document.pdf -cropcenter                              
 
         getQR.exe -f image.png -blob -cropcenter                           
 
         getQR.exe -f image.png -blob                                       
                   -bfilterw 0.15-0.30 -bfilterh 0.10-0.20 -bzone 0.5,0.5   
```
