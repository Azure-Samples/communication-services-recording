import React, { useState } from "react";
import { PrimaryButton, TextField } from 'office-ui-fabric-react';
import { CallKind } from "@azure/communication-calling";
import { createIdentifierFromRawId } from '@azure/communication-common';

export default function AddParticipantPopover({call}) {
    const [userId, setUserId] = useState('');

    function handleAddParticipant() {
        console.log('handleAddParticipant', userId);
        try {
            let participantId = createIdentifierFromRawId(userId);
            call._kind === CallKind.TeamsCall ? 
                call.addParticipant(participantId, {threadId}) :
                call.addParticipant(participantId);
        } catch (e) {
            console.error(e);
        }
    }
    return (
        <div className="ms-Grid">
            <div className="ms-Grid-row">
                <div className="ms-Grid-col ms-lg12">
                    <div className="add-participant-panel">
                        <h3 className="add-participant-panel-header">Add a participant</h3>
                        <div className="add-participant-panel-header">
                            <TextField className="text-left" label="Identifier" onChange={e => setUserId(e.target.value)} />
                            <PrimaryButton className="secondary-button mt-3" onClick={handleAddParticipant}>Add Participant</PrimaryButton>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}