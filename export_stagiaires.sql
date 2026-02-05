.headers on
.mode csv
.once stagiaires-dev.csv
SELECT Id,
       Nom,
       Prenom,
       Email,
       Telephone,
       Poste,
       SessionId,
       ClientId,
       SiteId,
       StatutInscription,
       HeuresPresence,
       EstPresent
FROM Stagiaires;